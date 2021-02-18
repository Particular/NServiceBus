namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_immediate_retries_with_native_transactions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_do_the_configured_number_of_retries_with_native_transactions()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((session, c) => session.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.ForwardedToErrorQueue)
                .Run();

            Assert.True(context.ForwardedToErrorQueue);
            Assert.AreEqual(numberOfRetries + 1, context.NumberOfTimesInvoked, "Message should be retried 5 times immediately");
            Assert.AreEqual(numberOfRetries, context.Logs.Count(l => l.Message
                .StartsWith($"Immediate Retry is going to retry message '{context.MessageId}' because of an exception:")));
        }

        const int numberOfRetries = 5;

        class Context : ScenarioContext
        {
            public int NumberOfTimesInvoked { get; set; }

            public bool ForwardedToErrorQueue { get; set; }

            public string MessageId { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var scenarioContext = (Context)context.ScenarioContext;
                    config.Recoverability().Failed(f => f.OnMessageSentToErrorQueue((message, _) =>
                    {
                        scenarioContext.ForwardedToErrorQueue = true;
                        return Task.FromResult(0);
                    }));

                    var recoverability = config.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(numberOfRetries));

                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    testContext.MessageId = context.MessageId;
                    testContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }

                Context testContext;
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}