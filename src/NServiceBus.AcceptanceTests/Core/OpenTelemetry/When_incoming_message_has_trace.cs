﻿namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
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
            Assert.AreEqual(2, incomingMessageActivities.Count, "2 messages are received as part of this test");
            Assert.AreEqual(2, outgoingMessageActivities.Count, "2 messages are sent as part of this test");

            var sendRequest = outgoingMessageActivities[0];
            var receiveRequest = incomingMessageActivities[0];
            var sendReply = outgoingMessageActivities[1];
            var receiveReply = incomingMessageActivities[1];

            Assert.AreEqual(sendRequest.RootId, receiveRequest.RootId, "first send operation is the root activity");
            Assert.AreEqual(sendRequest.Id, receiveRequest.ParentId, "first incoming message is correlated to the first send operation");
            Assert.AreEqual(sendRequest.RootId, sendReply.RootId, "first send operation is the root activity");
            Assert.AreEqual(sendReply.Id, receiveReply.ParentId, "second incoming message is correlated to the second send operation");
            Assert.AreEqual(sendRequest.RootId, receiveReply.RootId, "first send operation is the root activity");

            Assert.AreEqual(context.IncomingMessageId, sendRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
            Assert.AreEqual(context.IncomingMessageId, receiveRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
            Assert.AreEqual(context.ReplyMessageId, sendReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
            Assert.AreEqual(context.ReplyMessageId, receiveReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
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
            public ReceivingEndpoint() => EndpointSetup<DefaultServer>();

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
            public ReplyingEndpoint() => EndpointSetup<DefaultServer>();

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
            public TestEndpoint() => EndpointSetup<DefaultServer>();

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
}