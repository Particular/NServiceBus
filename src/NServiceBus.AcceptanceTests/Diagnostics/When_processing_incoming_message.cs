namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_processing_incoming_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_create_incoming_message_span()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            TestContext.WriteLine($"Created listener {activityListener.GetHashCode()}");
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .When(s => s.SendLocal(new IncomingMessage())))
                .Done(c => c.IncomingMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var incomingMessageActivities = activityListener.CompletedActivities.GetIncomingActivities();
            Assert.AreEqual(1, incomingMessageActivities.Count, "1 message is being processed");

            var incomingActivity = incomingMessageActivities.Single();
            Assert.AreEqual(ActivityKind.Consumer, incomingActivity.Kind, "asynchronous receivers should use 'Consumer'");

            Assert.AreEqual(ActivityStatusCode.Ok, incomingActivity.Status);
            var destination = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint));
            Assert.AreEqual($"process", incomingActivity.DisplayName);

            var incomingActivityTags = incomingActivity.Tags.ToImmutableDictionary();

            VerifyTag("messaging.message_id", context.IncomingMessageId);
            VerifyTag("messaging.conversation_id", context.IncomingMessageConversationId);
            VerifyTag("messaging.operation", "process");
            VerifyTag("messaging.destination", destination);
            VerifyTag("messaging.message_payload_size_bytes", "222");

            //TODO: Also add transport/native message id?
            VerifyTag("nservicebus.message_id", context.IncomingMessageId);
            VerifyTag("nservicebus.correlation_id", context.ReceivedHeaders[Headers.CorrelationId]);
            VerifyTag("nservicebus.conversation_id", context.ReceivedHeaders[Headers.ConversationId]);
            VerifyTag("nservicebus.content_type", context.ReceivedHeaders[Headers.ContentType]);
            VerifyTag("nservicebus.enclosed_message_types", context.ReceivedHeaders[Headers.EnclosedMessageTypes]);
            VerifyTag("nservicebus.reply_to_address", context.ReceivedHeaders[Headers.ReplyToAddress]);
            VerifyTag("nservicebus.originating_machine", context.ReceivedHeaders[Headers.OriginatingMachine]);
            VerifyTag("nservicebus.originating_endpoint", context.ReceivedHeaders[Headers.OriginatingEndpoint]);
            VerifyTag("nservicebus.version", context.ReceivedHeaders[Headers.NServiceBusVersion]);
            VerifyTag("nservicebus.message_intent", context.ReceivedHeaders[Headers.MessageIntent]);

            void VerifyTag(string tagKey, string expectedValue)
            {
                Assert.IsTrue(incomingActivityTags.TryGetValue(tagKey, out var tagValue), $"Tags should contain key {tagKey}");
                Assert.AreEqual(expectedValue, tagValue, $"Tag with key {tagKey} is incorrect");
            }
        }

        class Context : ScenarioContext
        {
            public string IncomingMessageId { get; set; }
            public string IncomingMessageConversationId { get; set; }
            public bool IncomingMessageReceived { get; set; }
            public IReadOnlyDictionary<string, string> ReceivedHeaders { get; set; }
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
                    if (context.MessageHeaders.TryGetValue(Headers.ConversationId, out var conversationId))
                    {
                        testContext.IncomingMessageConversationId = conversationId;
                    }
                    testContext.IncomingMessageReceived = true;
                    testContext.ReceivedHeaders = new Dictionary<string, string>(context.MessageHeaders.ToImmutableDictionary());
                    return Task.CompletedTask;
                }
            }
        }

        public class IncomingMessage : IMessage
        {
        }
    }
}