namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_sending_replies : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_outgoing_message_span()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s => s.SendLocal(new IncomingMessage())))
            .Run();

        var outgoingMessageActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.That(outgoingMessageActivities, Has.Count.EqualTo(2), "2 messages are being sent");
        var replyMessage = outgoingMessageActivities[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replyMessage.DisplayName, Is.EqualTo("reply"));
            Assert.That(replyMessage.RootId, Is.EqualTo(outgoingMessageActivities[0].RootId), "reply should belong to same trace as the triggering message");
            Assert.That(replyMessage.ParentId, Is.Not.Null, "reply should have ambient span");
        }

        replyMessage.VerifyUniqueTags();
    }

    public class Context : ScenarioContext
    {
        public string MessageConversationId { get; set; }
        public string OutgoingMessageId { get; set; }
    }

    public class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MessageHandler(Context testContext) : IHandleMessages<IncomingMessage>,
            IHandleMessages<OutgoingReply>
        {
            public Task Handle(IncomingMessage message, IMessageHandlerContext context) => context.Reply(new OutgoingReply());

            public Task Handle(OutgoingReply message, IMessageHandlerContext context)
            {
                testContext.MessageConversationId = context.MessageHeaders[Headers.ConversationId];
                testContext.OutgoingMessageId = context.MessageId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class IncomingMessage : IMessage;

    public class OutgoingReply : IMessage;
}