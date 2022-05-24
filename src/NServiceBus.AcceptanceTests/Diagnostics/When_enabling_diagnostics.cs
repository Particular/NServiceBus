namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_enabling_diagnostics : NServiceBusAcceptanceTest
    {
        //TODO test outgoing
        //TODO test correlation
        //TODO test "disabled" behavior?
        //TODO should these tests be moved to the Core test folder to not be shipped to downstreams?

        [Test]
        public async Task Should_capture_incoming_message_traces()
        {
            var activityListener = TestingActivityListener.Setup();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReceivingEndpoint)))
                    .When(s => s.Send(new IncomingMessage())))
                .WithEndpoint<ReceivingEndpoint>()
                .Done(c => c.IncomingMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var incomingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage");
            Assert.AreEqual(1, incomingMessageActivities.Count, "1 message is being processed");
            Assert.AreEqual(context.IncomingMessageId, incomingMessageActivities[0].Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["NServiceBus.MessageId"]);

            //TODO: Also add transport/native message id?
            //TODO should have no parent/correlation
        }

        [Test]
        public async Task Should_capture_outgoing_message_traces()
        {
            var activityListener = TestingActivityListener.Setup();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReceivingEndpoint)))
                    .When(s => s.Send(new IncomingMessage())))
                .WithEndpoint<ReceivingEndpoint>()
                .Done(c => c.IncomingMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var incomingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.OutgoingMessage");
            Assert.AreEqual(1, incomingMessageActivities.Count, "1 message is being sent");
            Assert.AreEqual(context.IncomingMessageId, incomingMessageActivities[0].Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["NServiceBus.MessageId"]);

            //TODO should have no parent/correlation
        }

        [Test]
        public async Task Should_correlate_traces()
        {
            var activityListener = TestingActivityListener.Setup();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .CustomConfig(c => c.ConfigureRouting().RouteToEndpoint(typeof(IncomingMessage), typeof(ReplyingEndpoint)))
                    .When(s => s.Send(new IncomingMessage())))
                .WithEndpoint<ReplyingEndpoint>()
                .Done(c => c.OutgoingMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");


            var incomingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage");
            var outgoingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.OutgoingMessage");
            Assert.AreEqual(2, incomingMessageActivities.Count, "2 messages are received as part of this test");
            Assert.AreEqual(2, outgoingMessageActivities.Count, "2 messages are sent as part of this test");

            //TODO not yet implemented
            // Assert.AreEqual(incomingMessageActivities[0].ParentId, outgoingMessageActivities[0].Id, "first incoming message is correlated to the first send operation");
            // Assert.AreEqual(incomingMessageActivities[0].RootId, outgoingMessageActivities[0].Id, "first send operation is the root activity");
            Assert.AreEqual(outgoingMessageActivities[1].ParentId, incomingMessageActivities[0].Id, "second send operation is correlated to the first incoming message");
            Assert.AreEqual(outgoingMessageActivities[1].RootId, incomingMessageActivities[0].Id, "first send operation is the root activity");
            Assert.AreEqual(incomingMessageActivities[1].ParentId, outgoingMessageActivities[1].Id, "second incoming message is correlated to the second send operation");
            Assert.AreEqual(incomingMessageActivities[1].RootId, outgoingMessageActivities[0].Id, "first send operation is the root activity");

            Assert.AreEqual(context.IncomingMessageId, outgoingMessageActivities[0].Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["NServiceBus.MessageId"]);
            Assert.AreEqual(context.IncomingMessageId, incomingMessageActivities[0].Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["NServiceBus.MessageId"]);
            Assert.AreEqual(context.OutgoingMessageId, outgoingMessageActivities[1].Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["NServiceBus.MessageId"]);
            Assert.AreEqual(context.OutgoingMessageId, incomingMessageActivities[1].Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["NServiceBus.MessageId"]);

            //TODO: Also add transport message id?
            //TODO: Test that the second send is connected to the first send
        }

        class Context : ScenarioContext
        {
            public bool OutgoingMessageReceived { get; set; }
            public string IncomingMessageId { get; set; }
            public string OutgoingMessageId { get; set; }
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
            public ReplyingEndpoint() => EndpointSetup<DefaultServer>(c => c.ConfigureRouting().RouteToEndpoint(typeof(OutgoingMessage), typeof(TestEndpoint)));

            class MessageHandler : IHandleMessages<IncomingMessage>
            {
                readonly Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(IncomingMessage message, IMessageHandlerContext context)
                {
                    testContext.IncomingMessageId = context.MessageId;
                    testContext.IncomingMessageReceived = true;
                    return context.Send(new OutgoingMessage()); //TODO change this to reply
                }
            }
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint() => EndpointSetup<DefaultServer>();

            class MessageHandler : IHandleMessages<OutgoingMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
                {
                    testContext.OutgoingMessageId = context.MessageId;
                    testContext.OutgoingMessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class IncomingMessage : IMessage
        {

        }

        public class OutgoingMessage : IMessage
        {

        }
    }
}