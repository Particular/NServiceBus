namespace NServiceBus.AcceptanceTests.Pipeline;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_message_is_faulted : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_flow_causation_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<CausationEndpoint>(b => b.When(session => session.SendLocal(new FirstMessage())).DoNotFailOnErrorMessages())
            .WithEndpoint<EndpointThatHandlesErrorMessages>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.RelatedTo, Is.EqualTo(context.OriginRelatedTo), "The RelatedTo header in fault message should be be equal to RelatedTo header in origin.");
            Assert.That(context.ConversationId, Is.EqualTo(context.OriginConversationId), "The ConversationId header in fault message should be be equal to ConversationId header in origin.");
        }
    }

    public class Context : ScenarioContext
    {
        public string RelatedTo { get; set; }
        public string ConversationId { get; set; }
        public string OriginRelatedTo { get; set; }
        public string OriginConversationId { get; set; }
    }

    public class CausationEndpoint : EndpointConfigurationBuilder
    {
        public CausationEndpoint() => EndpointSetup<DefaultServer>(c => c.SendFailedMessagesTo<EndpointThatHandlesErrorMessages>());

        public class FirstMessageHandler(Context testContext) : IHandleMessages<FirstMessage>
        {
            public Task Handle(FirstMessage message, IMessageHandlerContext context)
            {
                testContext.OriginRelatedTo = context.MessageId;
                testContext.OriginConversationId = context.MessageHeaders.ContainsKey(Headers.ConversationId) ? context.MessageHeaders[Headers.ConversationId] : null;

                return context.SendLocal(new MessageThatFails());
            }
        }

        public class MessageSentInsideHandlersHandler : IHandleMessages<MessageThatFails>
        {
            public Task Handle(MessageThatFails message, IMessageHandlerContext context) => throw new SimulatedException();
        }
    }

    class EndpointThatHandlesErrorMessages : EndpointConfigurationBuilder
    {
        public EndpointThatHandlesErrorMessages() => EndpointSetup<DefaultServer>();

        class ErrorMessageHandler(Context testContext) : IHandleMessages<MessageThatFails>
        {
            public Task Handle(MessageThatFails message, IMessageHandlerContext context)
            {
                testContext.RelatedTo = context.MessageHeaders.ContainsKey(Headers.RelatedTo) ? context.MessageHeaders[Headers.RelatedTo] : null;
                testContext.ConversationId = context.MessageHeaders.ContainsKey(Headers.ConversationId) ? context.MessageHeaders[Headers.ConversationId] : null;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FirstMessage : IMessage;

    public class MessageThatFails : IMessage;
}