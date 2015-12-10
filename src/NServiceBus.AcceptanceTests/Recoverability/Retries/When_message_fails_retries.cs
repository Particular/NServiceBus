namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_message_fails_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_forward_message_to_error_queue()
        {
            var exception = Assert.Throws<AggregateException>(async () => await
                Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b => b
                        .When((bus, c) => bus.SendLocal(new MessageWhichFailsRetries())))
                    .Done(c => c.ForwardedToErrorQueue)
                    .Run())
                .ExpectFailedMessages();

            Assert.AreEqual(1, exception.FailedMessages.Count);
            var failedMessage = exception.FailedMessages.Single();

            var testContext = (Context)exception.ScenarioContext;
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
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                });
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long)x);
                return unchecked((byte)longSum);
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public Task Start(IBusSession session)
                {
                    BusNotifications.Errors.MessageSentToErrorQueue += (sender, message) => Context.ForwardedToErrorQueue = true;
                    return Task.FromResult(0);
                }

                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
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

        public class Context : ScenarioContext
        {
            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class MessageWhichFailsRetries : IMessage
        {
        }
    }
}