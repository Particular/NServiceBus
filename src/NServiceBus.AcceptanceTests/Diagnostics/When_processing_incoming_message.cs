namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_processing_incoming_message
    {
        [Test]
        public async Task Should_create_incoming_message_span()
        {
            var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .When(s => s.SendLocal(new IncomingMessage())))
                .Done(c => c.IncomingMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var incomingMessageActivities = activityListener.CompletedActivities.FindAll(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage");
            Assert.AreEqual(1, incomingMessageActivities.Count, "1 message is being processed");

            var incomingActivity = incomingMessageActivities.Single();
            Assert.AreEqual(context.IncomingMessageId, incomingActivity.Tags.ToImmutableDictionary()["NServiceBus.MessageId"]);
            Assert.AreEqual(ActivityKind.Consumer, incomingActivity.Kind, "asynchronous receivers should use 'Consumer'");

            Assert.AreEqual(ActivityStatusCode.Ok, incomingActivity.Status);

            //TODO: Also add transport/native message id?
            //TODO: verify necessary tags/etc. here...
        }

        class Context : ScenarioContext
        {
            public string IncomingMessageId { get; set; }
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

        public class IncomingMessage : IMessage
        {
        }
    }
}