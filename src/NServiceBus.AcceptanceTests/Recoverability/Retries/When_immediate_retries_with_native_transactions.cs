namespace NServiceBus.AcceptanceTests.Recoverability.Retries
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
            Assert.AreEqual(5 + 1, context.NumberOfTimesInvoked, "Message should be retried 5 times immediately");
            Assert.AreEqual(5, context.Logs.Count(l => l.Message
                .StartsWith($"Immediate Retry is going to retry message '{context.MessageId}' because of an exception:")));
        }

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
                    var scenarioContext = (Context) context.ScenarioContext;
                    config.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => scenarioContext.ForwardedToErrorQueue = true;

                    var recoverability = config.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(5));
                    recoverability.Delayed(delayed => delayed.NumberOfRetries(0)); //disable the delayed retries

                    config.UseTransport(context.GetTransportType())
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    TestContext.MessageId = context.MessageId;
                    TestContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}