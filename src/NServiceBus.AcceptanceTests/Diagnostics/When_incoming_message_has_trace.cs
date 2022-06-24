namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_incoming_message_has_trace : NServiceBusAcceptanceTest // assuming W3C trace!
    {
        [Test]
        public async Task Should_correlate_trace_from_send()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReplyingEndpoint)))
                    .When(s => s.Send(new IncomingMessage())))
                .WithEndpoint<ReplyingEndpoint>()
                .Done(c => c.ReplyMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var incomingMessageActivities = activityListener.CompletedActivities.GetIncomingActivities();
            var outgoingMessageActivities = activityListener.CompletedActivities.GetOutgoingActivities();
            Assert.AreEqual(2, incomingMessageActivities.Count, "2 messages are received as part of this test");
            Assert.AreEqual(2, outgoingMessageActivities.Count, "2 messages are sent as part of this test");

            var sendRequest = outgoingMessageActivities[0];
            var receiveRequest = incomingMessageActivities[0];
            var sendReply = outgoingMessageActivities[1];
            var receiveReply = incomingMessageActivities[1];

            Assert.AreEqual(sendRequest.Id, receiveRequest.ParentId, "first incoming message is correlated to the first send operation");
            Assert.AreEqual(sendRequest.RootId, receiveRequest.RootId, "first send operation is the root activity");
            // TODO: Determine if this is still needed. Introduction of a handler invocation trace breaks this. Incoming->Handler Invocation->Outgoing
            // Assert.AreEqual(receiveRequest.Id, sendReply.ParentId, "second send operation is correlated to the first incoming message");
            Assert.AreEqual(sendRequest.RootId, sendReply.RootId, "first send operation is the root activity");
            Assert.AreEqual(sendReply.Id, receiveReply.ParentId, "second incoming message is correlated to the second send operation");
            Assert.AreEqual(sendRequest.RootId, receiveReply.RootId, "first send operation is the root activity");

            Assert.AreEqual(context.IncomingMessageId, sendRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
            Assert.AreEqual(context.IncomingMessageId, receiveRequest.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
            Assert.AreEqual(context.ReplyMessageId, sendReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);
            Assert.AreEqual(context.ReplyMessageId, receiveReply.Tags.ToImmutableDictionary()["nservicebus.message_id"]);

            //TODO: Also add transport message id?
            //TODO: Test that the second send is connected to the first send
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