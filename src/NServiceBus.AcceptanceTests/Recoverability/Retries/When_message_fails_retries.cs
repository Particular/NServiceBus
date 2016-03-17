namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_message_fails_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_forward_message_to_error_queue()
        {
            MessagesFailedException exception = null;
            try
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b => b
                        .When((session, c) => session.SendLocal(new MessageWhichFailsRetries())))
                    .Done(c => c.ForwardedToErrorQueue)
                    .Run();
            }
            catch (AggregateException ex)
            {
                exception = ex.ExpectFailedMessages();
            }

            Assert.AreEqual(1, exception.FailedMessages.Count);
            var failedMessage = exception.FailedMessages.Single();

            var testContext = (Context) exception.ScenarioContext;
            Assert.AreEqual(typeof(MessageWhichFailsRetries).AssemblyQualifiedName, failedMessage.Headers[Headers.EnclosedMessageTypes]);
            Assert.AreEqual(testContext.PhysicalMessageId, failedMessage.MessageId);
            Assert.IsAssignableFrom(typeof(SimulatedException), failedMessage.Exception);

            Assert.AreEqual(1, testContext.Logs.Count(l => l.Message
                .StartsWith($"Moving message '{testContext.PhysicalMessageId}' to the error queue because processing failed due to an exception:")));
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                    configure.GetSettings().Get<Notifications>().Errors.MessageSentToErrorQueue += (sender, message) => scenarioContext.ForwardedToErrorQueue = true;
                });
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long) x);
                return unchecked((byte) longSum);
            }

            class MessageHandler : IHandleMessages<MessageWhichFailsRetries>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageWhichFailsRetries message, IMessageHandlerContext context)
                {
                    TestContext.PhysicalMessageId = context.MessageId;
                    throw new SimulatedException();
                }
            }
        }

        class Context : ScenarioContext
        {
            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class MessageWhichFailsRetries : IMessage
        {
        }
    }
}