namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_retrying_message_from_error_queue : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_confirm_successful_processing()
        {
            var retryId = Guid.NewGuid().ToString("D");

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ProcessingEndpoint>(e => e
                    .When(s =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.RouteToThisEndpoint();
                        // set SC retry header information
                        sendOptions.SetHeader("ServiceControl.Retry.UniqueMessageId", retryId);
                        sendOptions.SetHeader("ServiceControl.Retry.AcknowledgementQueue", Conventions.EndpointNamingConvention(typeof(RetryAckSpy)));
                        return s.Send(new FailedMessage(), sendOptions);
                    }))
                .WithEndpoint<RetryAckSpy>()
                .WithEndpoint<AuditSpy>()
                .Done(c => c.ConfirmedRetryId != null && c.AuditHeaders != null)
                .Run();

            Assert.IsTrue(context.MessageProcessed);
            Assert.AreEqual(retryId, context.ConfirmedRetryId);
            var processingTime = DateTimeExtensions.ToUtcDateTime(context.RetryProcessingTimestamp);
            Assert.That(processingTime, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.IsTrue(context.AuditHeaders.ContainsKey("ServiceControl.Retry.AcknowledgementSent"));
        }

        class Context : ScenarioContext
        {
            public string ConfirmedRetryId { get; set; }
            public string RetryProcessingTimestamp { get; set; }
            public bool MessageProcessed { get; set; }
            public IReadOnlyDictionary<string, string> AuditHeaders { get; set; }
        }

        class ProcessingEndpoint : EndpointConfigurationBuilder
        {
            public ProcessingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.AuditProcessedMessagesTo<AuditSpy>();
                });
            }

            class FailedMessageHandler : IHandleMessages<FailedMessage>
            {
                Context testContext;

                public FailedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailedMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageProcessed = true;
                    return Task.FromResult(0);
                }
            }
        }

        class RetryAckSpy : EndpointConfigurationBuilder
        {
            public RetryAckSpy() => EndpointSetup<DefaultServer>((e, r) => e.Pipeline.Register(
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

        class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy() => EndpointSetup<DefaultServer>();

            class FailedMessageHandler : IHandleMessages<FailedMessage>
            {
                Context testContext;

                public FailedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailedMessage message, IMessageHandlerContext context)
                {
                    testContext.AuditHeaders = context.MessageHeaders;
                    return Task.FromResult(0);
                }
            }

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

        public class FailedMessage : IMessage
        {
        }
    }
}