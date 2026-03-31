namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_using_custom_conversation_id : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_apply_custom_conversation_id_when_no_incoming_message()
    {
        var customConversationId = Guid.NewGuid().ToString();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(e => e
                .When(s =>
                {
                    var options = new SendOptions();
                    options.RouteToThisEndpoint();
                    options.SetHeader(Headers.ConversationId, customConversationId);
                    return s.Send(new MessageWithConversationId(), options);
                }))
            .Run();

        Assert.That(context.ReceivedConversationId, Is.EqualTo(customConversationId));
    }

    public class Context : ScenarioContext
    {
        public string ReceivedConversationId { get; set; }
    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class ConversationIdMessageHandler(Context testContext) : IHandleMessages<MessageWithConversationId>
        {
            public Task Handle(MessageWithConversationId message, IMessageHandlerContext context)
            {
                testContext.ReceivedConversationId = context.MessageHeaders[Headers.ConversationId];
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MessageWithConversationId : ICommand;
}