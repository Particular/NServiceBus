namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_disabling_payload_restrictions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_messages_above_64kb()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<LargePayloadEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage
                {
                    LargeProperty = new byte[1024 * 64]
                })))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived, "Message was not received");
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class LargePayloadEndpoint : EndpointConfigurationBuilder
        {
            public LargePayloadEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<LearningTransport>()
                        .NoPayloadSizeRestriction();
                });
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }

                public SomeMessageHandler(Context testContext)
                {
                    TestContext = testContext;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    TestContext.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SomeMessage : IMessage
        {
            public byte[] LargeProperty { get; set; }
        }
    }
}