namespace NServiceBus.AcceptanceTests.Causation
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_overriding_conversation_id_generation : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_custom_id()
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
                EndpointSetup<DefaultServer>(c => c.CustomConversationId(delegate(CustomConversationIdContext context, out string id)
                {
                    if (context.Message.Instance is MessageSentOutsideOfHandler)
                    {
                        id = "custom id";
                        return true;
                    }

                    id = null;
                    return false;
                }));
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