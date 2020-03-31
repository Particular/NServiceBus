namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_message_fails_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_forward_message_to_error_queue()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b => b
                        .When((session, c) => session.SendLocal(new MessageWhichFailsRetries())))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.AreEqual(1, exception.ScenarioContext.FailedMessages.Count);

            var testContext = (Context) exception.ScenarioContext;
            Assert.AreEqual(typeof(MessageWhichFailsRetries).AssemblyQualifiedName, exception.FailedMessage.Headers[Headers.EnclosedMessageTypes]);
            Assert.AreEqual(testContext.PhysicalMessageId, exception.FailedMessage.MessageId);
            Assert.IsAssignableFrom(typeof(SimulatedException), exception.FailedMessage.Exception);

            Assert.AreEqual(1, testContext.Logs.Count(l => l.Message
                .StartsWith($"Moving message '{testContext.PhysicalMessageId}' to the error queue 'error' because processing failed due to an exception:")));
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MessageHandler : IHandleMessages<MessageWhichFailsRetries>
            {
                public Context TestContext { get; set; }

                public MessageHandler(Context testContext)
                {
                    TestContext = testContext;
                }

                public Task Handle(MessageWhichFailsRetries message, IMessageHandlerContext context)
                {
                    TestContext.PhysicalMessageId = context.MessageId;
                    throw new SimulatedException();
                }
            }
        }

        class Context : ScenarioContext
        {
            public string PhysicalMessageId { get; set; }
        }

        public class MessageWhichFailsRetries : IMessage
        {
        }
    }
}