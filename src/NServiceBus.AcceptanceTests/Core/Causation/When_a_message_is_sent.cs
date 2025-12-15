namespace NServiceBus.AcceptanceTests.Core.Causation;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_message_is_sent : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_flow_causation_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<CausationEndpoint>(b => b.When(session => session.SendLocal(new MessageSentOutsideOfHandler())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ConversationIdReceived, Is.EqualTo(context.FirstConversationId), "Conversation id should flow to outgoing messages");
            Assert.That(context.RelatedToReceived, Is.EqualTo(context.MessageIdOfFirstMessage), "RelatedToId on outgoing messages should be set to the message id of the message causing it to be sent");
        }
    }

    public class Context : ScenarioContext
    {
        public string FirstConversationId { get; set; }
        public string ConversationIdReceived { get; set; }
        public string MessageIdOfFirstMessage { get; set; }
        public string RelatedToReceived { get; set; }
    }

    public class CausationEndpoint : EndpointConfigurationBuilder
    {
        public CausationEndpoint() => EndpointSetup<DefaultServer>();

        public Context Context { get; set; }

        public class MessageSentOutsideHandlersHandler(Context testContext) : IHandleMessages<MessageSentOutsideOfHandler>
        {
            public Task Handle(MessageSentOutsideOfHandler message, IMessageHandlerContext context)
            {
                testContext.FirstConversationId = context.MessageHeaders[Headers.ConversationId];
                testContext.MessageIdOfFirstMessage = context.MessageId;

                return context.SendLocal(new MessageSentInsideHandler());
            }
        }

        public class MessageSentInsideHandlersHandler(Context testContext) : IHandleMessages<MessageSentInsideHandler>
        {
            public Task Handle(MessageSentInsideHandler message, IMessageHandlerContext context)
            {
                testContext.ConversationIdReceived = context.MessageHeaders[Headers.ConversationId];

                testContext.RelatedToReceived = context.MessageHeaders[Headers.RelatedTo];

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageSentOutsideOfHandler : IMessage;

    public class MessageSentInsideHandler : IMessage;
}