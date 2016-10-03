namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_delayed_retries_and_counting : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_reschedule_message_the_number_of_times_configured()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(session => session.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.ForwardedToErrorQueue)
                .Run(TimeSpan.FromSeconds(120));

            Assert.IsTrue(context.ForwardedToErrorQueue);
            Assert.AreEqual(ConfiguredNumberOfImmediateRetries, context.Logs.Count(l => l.Message
                .StartsWith($"Delayed Retry will reschedule message '{context.PhysicalMessageId}'")));
        }

        const int ConfiguredNumberOfImmediateRetries = 3;

        class Context : ScenarioContext
        {
            public bool ForwardedToErrorQueue { get; set; }
            public string PhysicalMessageId { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    configure.EnableFeature<TimeoutManager>();
                    configure.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => { scenarioContext.ForwardedToErrorQueue = true; };

                    var recoverability = configure.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
                    recoverability.Delayed(settings => settings.TimeIncrease(TimeSpan.FromMilliseconds(1)).NumberOfRetries(ConfiguredNumberOfImmediateRetries));
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    testContext.PhysicalMessageId = context.MessageId;
                    throw new SimulatedException();
                }

                Context testContext;
            }
        }

        
        public class MessageToBeRetried : IMessage
        {
        }
    }
}