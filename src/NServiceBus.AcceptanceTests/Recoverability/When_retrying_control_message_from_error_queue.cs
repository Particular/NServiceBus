namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;
    using Unicast.Transport;

    public class When_retrying_control_message_from_error_queue : NServiceBusAcceptanceTest
    {
        static readonly string RetryId = Guid.NewGuid().ToString("D");

        [Test]
        public async Task Should_confirm_successful_processing_to_error_queue()
        {
            Requires.MessageDrivenPubSub(); //required for subscription control message support

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ProcessingEndpoint>()
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.ConfirmedRetryId != null)
                .Run();

            Assert.AreEqual(RetryId, context.ConfirmedRetryId);
            var processingTime = DateTimeOffset.Parse(context.RetryProcessingTimestamp);
            Assert.That(processingTime, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromMinutes(1)));
        }

        class Context : ScenarioContext
        {
            public string ConfirmedRetryId { get; set; }
            public string RetryProcessingTimestamp { get; set; }
        }

        class ProcessingEndpoint : EndpointConfigurationBuilder
        {
            public ProcessingEndpoint() => EndpointSetup<DefaultServer>(c =>
            {
                c.EnableFeature<ControlMessageFeature>();
                c.SendFailedMessagesTo<ErrorSpy>();
            });

            class ControlMessageFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(s =>
                        new ControlMessageSender(s.GetRequiredService<IMessageDispatcher>()));
                }
            }

            class ControlMessageSender : FeatureStartupTask
            {
                IMessageDispatcher dispatcher;

                public ControlMessageSender(IMessageDispatcher dispatcher)
                {
                    this.dispatcher = dispatcher;
                }

                protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);
                    // set necessary subscription control message headers
                    controlMessage.Headers.Add(Headers.SubscriptionMessageType, typeof(object).AssemblyQualifiedName);
                    controlMessage.Headers.Add(Headers.ReplyToAddress, "TestSubscriberAddress");
                    // set SC headers
                    controlMessage.Headers.Add("ServiceControl.Retry.UniqueMessageId", RetryId);
                    controlMessage.Headers.Add("ServiceControl.Version", Math.PI.ToString("N"));
                    var messageOperation = new TransportOperation(controlMessage, new UnicastAddressTag(Conventions.EndpointNamingConvention(typeof(ProcessingEndpoint))));
                    await dispatcher.Dispatch(new TransportOperations(messageOperation), new TransportTransaction(), cancellationToken);
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy() => EndpointSetup<DefaultServer>((e, r) => e.Pipeline.Register(
                new ControlMessageBehavior(r.ScenarioContext as Context),
                "Checks for confirmation control message"));

            class ControlMessageBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public ControlMessageBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    await next();

                    testContext.ConfirmedRetryId = context.MessageHeaders["ServiceControl.Retry.UniqueMessageId"];
                    testContext.RetryProcessingTimestamp = context.MessageHeaders["ServiceControl.Retry.Successful"];
                }
            }
        }
    }
}