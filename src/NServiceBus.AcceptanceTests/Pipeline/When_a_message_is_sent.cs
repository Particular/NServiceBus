namespace NServiceBus.AcceptanceTests.Pipeline
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_sent : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_flow_causation_headers()
        {
            var context = await new Scenario<Context>()
                .WithEndpoint<CausationEndpoint>(b => b.When(session => session.SendLocal(new MessageSentOutsideOfHandler())))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(context.FirstConversationId, context.ConversationIdReceived, "Conversation id should flow to outgoing messages");
            Assert.AreEqual(context.MessageIdOfFirstMessage, context.RelatedToReceived, "RelatedToId on outgoing messages should be set to the message id of the message causing it to be sent");
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string FirstConversationId { get; set; }
            public string ConversationIdReceived { get; set; }
            public string MessageIdOfFirstMessage { get; set; }
            public string RelatedToReceived { get; set; }
        }

        public class CausationEndpoint : EndpointConfigurationBuilder
        {
            public CausationEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public Context Context { get; set; }

            public class MessageSentOutsideHandlersHandler : IHandleMessages<MessageSentOutsideOfHandler>
            {
                public Task Handle(MessageSentOutsideOfHandler message, IMessageHandlerContext context)
                {
                    var testContext = Scenario<Context>.CurrentContext.Value;

                    testContext.FirstConversationId = context.MessageHeaders[Headers.ConversationId];
                    testContext.MessageIdOfFirstMessage = context.MessageId;

                    return context.SendLocal(new MessageSentInsideHandler());
                }
            }

            public class MessageSentInsideHandlersHandler : IHandleMessages<MessageSentInsideHandler>
            {
                public Task Handle(MessageSentInsideHandler message, IMessageHandlerContext context)
                {
                    var testContext = Scenario<Context>.CurrentContext.Value;

                    testContext.ConversationIdReceived = context.MessageHeaders[Headers.ConversationId];

                    testContext.RelatedToReceived = context.MessageHeaders[Headers.RelatedTo];

                    testContext.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageSentOutsideOfHandler : IMessage
        {
        }

        public class MessageSentInsideHandler : IMessage
        {
        }
    }
}