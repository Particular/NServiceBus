namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
            .Done(c => c.OutgoingMessageReceived)
            .Run();

        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.That(outgoingMessageActivities.Count, Is.EqualTo(1), "1 message is being sent");
        var sentMessage = outgoingMessageActivities.Single();

        Assert.IsNull(sentMessage.ParentId, "sends without ambient span should start a new trace");
        Assert.That(sentMessage.DisplayName, Is.EqualTo("send message"));

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
                .When(s =>
                {
                    return s.SendLocal(new OutgoingMessage());
                }))
            .Done(c => c.OutgoingMessageReceived)
            .Run();

        var sendMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        Assert.That(sendMessageActivities.Count, Is.EqualTo(1), "1 message is sent as part of this test");
        Assert.That(receiveMessageActivities.Count, Is.EqualTo(1), "1 message is received as part of this test");

        var sendRequest = sendMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        Assert.That(receiveRequest.RootId, Is.EqualTo(sendRequest.RootId), "send and receive operations are part of the same root activity");
        Assert.IsNotNull(receiveRequest.ParentId, "incoming message does have a parent");

        CollectionAssert.IsEmpty(receiveRequest.Links, "receive does not have links");
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
            .Done(c => c.OutgoingMessageReceived)
            .Run();

        var sendMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        Assert.That(sendMessageActivities.Count, Is.EqualTo(1), "1 message is sent as part of this test");
        Assert.That(receiveMessageActivities.Count, Is.EqualTo(1), "1 message is received as part of this test");

        var sendRequest = sendMessageActivities[0];
        var receiveRequest = receiveMessageActivities[0];

        Assert.AreNotEqual(sendRequest.RootId, receiveRequest.RootId, "send and receive operations are part of different root activities");
        Assert.IsNull(receiveRequest.ParentId, "incoming message does not have a parent, it's a root");

        ActivityLink link = receiveRequest.Links.FirstOrDefault();
        Assert.IsNotNull(link, "Receive has a link");
        Assert.That(link.Context.TraceId, Is.EqualTo(sendRequest.TraceId), "receive is linked to send operation");
    }

    class Context : ScenarioContext
    {
        public bool OutgoingMessageReceived { get; set; }
        public string SentMessageId { get; set; }
        public string MessageConversationId { get; set; }
        public IReadOnlyDictionary<string, string> SentMessageHeaders { get; set; }
    }


    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<OutgoingMessage>
        {
            Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                testContext.SentMessageId = context.MessageId;
                testContext.MessageConversationId = context.MessageHeaders[Headers.ConversationId];
                testContext.OutgoingMessageReceived = true;
                testContext.SentMessageHeaders = new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                return Task.CompletedTask;
            }
        }
    }

    public class OutgoingMessage : IMessage
    {
    }
}