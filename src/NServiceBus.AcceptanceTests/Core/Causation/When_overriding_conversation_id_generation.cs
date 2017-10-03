namespace NServiceBus.AcceptanceTests.Causation
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_overriding_conversation_id_generation : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_custom_id()
        {
            var myBusinessMessage = new MessageSentOutsideOfHandlerMatchingTheConvention
            {
                MyBusinessId = "some id"
            };

            var context = await Scenario.Define<Context>()
                .WithEndpoint<CustomGeneratorEndpoint>(b => b.When(async session =>
                {
                    await session.SendLocal(myBusinessMessage);

                    await session.SendLocal(new MessageSentOutsideOfHandlerNotMatchingTheConvention());
                }))
                .Done(c => c.MatchingMessageReceived && c.NonMatchingMessageReceived)
                .Run();

            Assert.AreEqual(myBusinessMessage.MyBusinessId, context.MatchingConversationIdReceived);
            Assert.True(Guid.TryParse(context.NonMatchingConversationIdReceived, out var _));
        }

        public class Context : ScenarioContext
        {
            public string MatchingConversationIdReceived { get; set; }
            public bool MatchingMessageReceived { get; set; }
            public string NonMatchingConversationIdReceived { get; set; }
            public bool NonMatchingMessageReceived { get; set; }
        }

        public class CustomGeneratorEndpoint : EndpointConfigurationBuilder
        {
            public CustomGeneratorEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.CustomConversationId(MyCustomConversationIdStrategy));
            }

            bool MyCustomConversationIdStrategy(CustomConversationIdContext context, out string conversationId)
            {
                if (context.Message.Instance is MessageSentOutsideOfHandlerMatchingTheConvention message)
                {
                    conversationId = message.MyBusinessId;
                    return true;
                }

                conversationId = null;
                return false;
            }

            public Context Context { get; set; }

            public class MessageSentOutsideOfHandlerMatchingTheConventionHandler : IHandleMessages<MessageSentOutsideOfHandlerMatchingTheConvention>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageSentOutsideOfHandlerMatchingTheConvention message, IMessageHandlerContext context)
                {
                    TestContext.MatchingConversationIdReceived = context.MessageHeaders[Headers.ConversationId];
                    TestContext.MatchingMessageReceived = true;
                    return Task.FromResult(0);
                }
            }

            public class MessageSentOutsideOfHandlerNotMatchingTheConventionHandler : IHandleMessages<MessageSentOutsideOfHandlerNotMatchingTheConvention>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageSentOutsideOfHandlerNotMatchingTheConvention message, IMessageHandlerContext context)
                {
                    TestContext.NonMatchingConversationIdReceived = context.MessageHeaders[Headers.ConversationId];
                    TestContext.NonMatchingMessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageSentOutsideOfHandlerMatchingTheConvention : IMessage
        {
            public string MyBusinessId { get; set; }
        }

        public class MessageSentOutsideOfHandlerNotMatchingTheConvention : IMessage
        {
        }
    }
}