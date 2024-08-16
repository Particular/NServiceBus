namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_incoming_message_has_trace : OpenTelemetryAcceptanceTest // assuming W3C trace!
{
    [Test]
    public async Task Should_correlate_trace_from_send()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReplyingEndpoint)))
                .When(s => s.Send(new IncomingMessage())))
            .WithEndpoint<ReplyingEndpoint>()
            .Done(c => c.ReplyMessageReceived)
            .Run();

        var incomingMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
        Assert.That(incomingMessageActivities.Count, Is.EqualTo(2), "2 messages are received as part of this test");
        Assert.That(outgoingMessageActivities.Count, Is.EqualTo(2), "2 messages are sent as part of this test");

        var sendRequest = outgoingMessageActivities[0];
        var receiveRequest = incomingMessageActivities[0];
        var sendReply = outgoingMessageActivities[1];
        var receiveReply = incomingMessageActivities[1];

        Assert.That(receiveRequest.RootId, Is.EqualTo(sendRequest.RootId), "first send operation is the root activity");
        Assert.That(receiveRequest.ParentId, Is.EqualTo(sendRequest.Id), "first incoming message is correlated to the first send operation");
        Assert.That(sendReply.RootId, Is.EqualTo(sendRequest.RootId), "first send operation is the root activity");
        Assert.That(receiveReply.ParentId, Is.EqualTo(sendReply.Id), "second incoming message is correlated to the second send operation");
        Assert.That(receiveReply.RootId, Is.EqualTo(sendRequest.RootId), "first send operation is the root activity");

        Assert.That(sendRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"], Is.EqualTo(context.IncomingMessageId));
        Assert.That(receiveRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"], Is.EqualTo(context.IncomingMessageId));
        Assert.That(sendReply.Tags.ToImmutableDictionary()["nservicebus.message_id"], Is.EqualTo(context.ReplyMessageId));
        Assert.That(receiveReply.Tags.ToImmutableDictionary()["nservicebus.message_id"], Is.EqualTo(context.ReplyMessageId));
    }

    class Context : ScenarioContext
    {
        public bool ReplyMessageReceived { get; set; }
        public string IncomingMessageId { get; set; }
        public string ReplyMessageId { get; set; }
        public bool IncomingMessageReceived { get; set; }
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<IncomingMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(IncomingMessage message, IMessageHandlerContext context)
            {
                testContext.IncomingMessageId = context.MessageId;
                testContext.IncomingMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    class ReplyingEndpoint : EndpointConfigurationBuilder
    {
        public ReplyingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<IncomingMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(IncomingMessage message, IMessageHandlerContext context)
            {
                testContext.IncomingMessageId = context.MessageId;
                testContext.IncomingMessageReceived = true;
                return context.Reply(new ReplyMessage());
            }
        }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class MessageHandler : IHandleMessages<ReplyMessage>
        {
            Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyMessageId = context.MessageId;
                testContext.ReplyMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class IncomingMessage : IMessage
    {
    }

    public class ReplyMessage : IMessage
    {
    }
}