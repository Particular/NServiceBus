namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_message_fails_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Should_forward_message_to_error_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((bus, c) => bus.SendLocal(new MessageWhichFailsRetries())))
                .Done(c => c.FailedMessage != null)
                .Run();

            var failedMessage = context.FailedMessage.Value;
            Assert.AreEqual(typeof(MessageWhichFailsRetries).AssemblyQualifiedName, failedMessage.Headers[Headers.EnclosedMessageTypes]);
            Assert.AreEqual(context.PhysicalMessageId, failedMessage.MessageId);
            Assert.IsAssignableFrom(typeof(SimulatedException), failedMessage.Exception);

            Assert.AreEqual(1, context.Logs.Count(l => l.Message
                .StartsWith($"Moving message '{context.PhysicalMessageId}' to the error queue because processing failed due to an exception:")));
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                    var context = (Context)ScenarioContext;
                    configure.NotifyOnFailedMessage(message => context.FailedMessage = message);
                });
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
            public string PhysicalMessageId { get; set; }
            public FailedMessage? FailedMessage { get; set; }
        }

        public class MessageWhichFailsRetries : IMessage
        {
        }
    }
}