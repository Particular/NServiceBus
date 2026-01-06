namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;

public class When_sending_messages : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_outgoing_message_span()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s => s.SendLocal(new OutgoingMessage())))
            .Run();

        var outgoingMessageActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.That(outgoingMessageActivities, Has.Count.EqualTo(1), "1 message is being sent");
        var sentMessage = outgoingMessageActivities.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sentMessage.ParentId, Is.Null, "sends without ambient span should start a new trace");
            Assert.That(sentMessage.DisplayName, Is.EqualTo("send message"));
        }

        var sentMessageTags = sentMessage.Tags.ToImmutableDictionary();
        sentMessageTags.VerifyTag("nservicebus.message_id", context.SentMessageId);
        sentMessageTags.VerifyTag("nservicebus.correlation_id", context.SentMessageHeaders[Headers.CorrelationId]);
        sentMessageTags.VerifyTag("nservicebus.conversation_id", context.SentMessageHeaders[Headers.ConversationId]);
        sentMessageTags.VerifyTag("nservicebus.content_type", context.SentMessageHeaders[Headers.ContentType]);
        sentMessageTags.VerifyTag("nservicebus.enclosed_message_types", context.SentMessageHeaders[Headers.EnclosedMessageTypes]);
        sentMessageTags.VerifyTag("nservicebus.reply_to_address", context.SentMessageHeaders[Headers.ReplyToAddress]);
        sentMessageTags.VerifyTag("nservicebus.originating_machine", context.SentMessageHeaders[Headers.OriginatingMachine]);
        sentMessageTags.VerifyTag("nservicebus.originating_endpoint", context.SentMessageHeaders[Headers.OriginatingEndpoint]);
        sentMessageTags.VerifyTag("nservicebus.version", context.SentMessageHeaders[Headers.NServiceBusVersion]);
        sentMessageTags.VerifyTag("nservicebus.message_intent", context.SentMessageHeaders[Headers.MessageIntent]);
    }

    [Test]
    public async Task Should_create_new_child_on_receive_by_default()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s => s.SendLocal(new OutgoingMessage())))
            .Run();

        var sendMessageActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveMessageActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendMessageActivities, Has.Count.EqualTo(1), "1 message is sent as part of this test");
            Assert.That(receiveMessageActivities, Has.Count.EqualTo(1), "1 message is received as part of this test");
        }

        var sendRequest = sendMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receiveRequest.RootId, Is.EqualTo(sendRequest.RootId), "send and receive operations are part of the same root activity");
            Assert.That(receiveRequest.ParentId, Is.Not.Null, "incoming message does have a parent");
        }

        Assert.That(receiveRequest.Links, Is.Empty, "receive does not have links");
    }

    [Test]
    public async Task Should_create_new_linked_trace_on_receive_when_requested_via_options()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.StartNewTraceOnReceive();
                    return s.Send(new OutgoingMessage(), sendOptions);
                }))
            .Run();

        var sendMessageActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveMessageActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendMessageActivities, Has.Count.EqualTo(1), "1 message is sent as part of this test");
            Assert.That(receiveMessageActivities, Has.Count.EqualTo(1), "1 message is received as part of this test");
        }

        var sendRequest = sendMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receiveRequest.RootId, Is.Not.EqualTo(sendRequest.RootId), "send and receive operations are part of different root activities");
            Assert.That(receiveRequest.ParentId, Is.Null, "incoming message does not have a parent, it's a root");
        }

        ActivityLink link = receiveRequest.Links.FirstOrDefault();
        Assert.That(link, Is.Not.EqualTo(default(ActivityLink)), "Receive has a link");
        Assert.That(link.Context.TraceId, Is.EqualTo(sendRequest.TraceId), "receive is linked to send operation");
    }

    class Context : ScenarioContext
    {
        public string SentMessageId { get; set; }
        public string MessageConversationId { get; set; }
        public Dictionary<string, string> SentMessageHeaders { get; set; }
    }


    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<DefaultServer>();

        class MessageHandler(Context testContext) : IHandleMessages<OutgoingMessage>
        {
            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                testContext.SentMessageId = context.MessageId;
                testContext.MessageConversationId = context.MessageHeaders[Headers.ConversationId];
                testContext.SentMessageHeaders = new Dictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage;
}