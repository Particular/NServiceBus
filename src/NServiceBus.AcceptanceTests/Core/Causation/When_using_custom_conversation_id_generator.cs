namespace NServiceBus.AcceptanceTests.Causation
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_custom_conversation_id_generator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_custom_generator()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<CustomGeneratorEndpoint>(b => b.When(session => session.SendLocal(new MessageSentOutsideOfHandler())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual(context.ConversationIdReceived, "custom id");
        }

        public class Context : ScenarioContext
        {
            public string ConversationIdReceived { get; set; }
            public bool MessageReceived { get; set; }
        }

        public class CustomGeneratorEndpoint : EndpointConfigurationBuilder
        {
            public CustomGeneratorEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.CustomConversationIdGenerator(_ => "custom id"));
            }

            public Context Context { get; set; }

            public class MessageSentOutsideHandlersHandler : IHandleMessages<MessageSentOutsideOfHandler>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageSentOutsideOfHandler message, IMessageHandlerContext context)
                {
                    TestContext.ConversationIdReceived = context.MessageHeaders[Headers.ConversationId];
                    TestContext.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageSentOutsideOfHandler : IMessage
        {
        }
    }
}