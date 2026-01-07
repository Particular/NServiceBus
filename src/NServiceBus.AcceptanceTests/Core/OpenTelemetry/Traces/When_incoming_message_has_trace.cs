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
            .Run();

        var incomingMessageActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var outgoingMessageActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(incomingMessageActivities, Has.Count.EqualTo(2), "2 messages are received as part of this test");
            Assert.That(outgoingMessageActivities, Has.Count.EqualTo(2), "2 messages are sent as part of this test");
        }

        var sendRequest = outgoingMessageActivities[0];
        var receiveRequest = incomingMessageActivities[0];
        var sendReply = outgoingMessageActivities[1];
        var receiveReply = incomingMessageActivities[1];

        using (Assert.EnterMultipleScope())
        {
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
    }

    class Context : ScenarioContext
    {
        public string IncomingMessageId { get; set; }
        public string ReplyMessageId { get; set; }
        public bool IncomingMessageReceived { get; set; }
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultServer>();

        class MessageHandler(Context testContext) : IHandleMessages<IncomingMessage>
        {
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
        public ReplyingEndpoint() => EndpointSetup<DefaultServer>();

        class MessageHandler(Context testContext) : IHandleMessages<IncomingMessage>
        {
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
        public TestEndpoint() => EndpointSetup<DefaultServer>();

        class MessageHandler(Context testContext) : IHandleMessages<ReplyMessage>
        {
            public Task Handle(ReplyMessage message, IMessageHandlerContext context)
            {
                testContext.ReplyMessageId = context.MessageId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class IncomingMessage : IMessage;

    public class ReplyMessage : IMessage;
}