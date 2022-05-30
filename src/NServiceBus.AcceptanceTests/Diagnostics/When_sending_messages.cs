namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_sending_messages : NServiceBusAcceptanceTest
    {
        //TODO test "disabled" behavior?
        //TODO should these tests be moved to the Core test folder to not be shipped to downstreams?

        [Test]
        public async Task Should_create_outgoing_message_span()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .When(s => s.SendLocal(new OutgoingMessage())))
                .Done(c => c.OutgoingMessageReceived)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var outgoingMessageActivities = activityListener.CompletedActivities.GetOutgoingActivities();
            Assert.AreEqual(1, outgoingMessageActivities.Count, "1 message is being sent");
            var sentMessage = outgoingMessageActivities.Single();

            Assert.IsNull(sentMessage.ParentId, "sends without ambient span should start a new trace");
            var destination = Conventions.EndpointNamingConvention(typeof(TestEndpoint));
            Assert.AreEqual($"{destination} send", sentMessage.DisplayName, "Display name should be set according to spec");


            var sentMessageTags = sentMessage.Tags.ToImmutableDictionary();
            // TODO: Verify whether we want to keep this. messaging.message_id is from the spec
            VerifyTag("NServiceBus.MessageId", context.SentMessageId);
            VerifyTag("messaging.message_id", context.SentMessageId); // TODO: should be set by the transport? If set by NSB, this should be the transport message id?
            VerifyTag("messaging.conversation_id", context.MessageConversationId);
            VerifyTag("messaging.operation", "send");
            VerifyTag("messaging.destination_kind", "queue");
            VerifyTag("messaging.destination", destination);
            // NOTE: Payload size is zero and the tag is not added
            VerifyTag("messaging.message_payload_size_bytes", "222");

            void VerifyTag(string tagKey, string expectedValue)
            {
                Assert.IsTrue(sentMessageTags.TryGetValue(tagKey, out var tagValue), $"Tags should contain key {tagKey}");
                Assert.AreEqual(expectedValue, tagValue, $"Tag with key {tagKey} is incorrect");
            }
        }

        class Context : ScenarioContext
        {
            public bool OutgoingMessageReceived { get; set; }
            public string SentMessageId { get; set; }
            public string MessageConversationId { get; set; }
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
                    testContext.SentMessageId = context.MessageId;
                    testContext.MessageConversationId = context.MessageHeaders[Headers.ConversationId];
                    testContext.OutgoingMessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class OutgoingMessage : IMessage
        {
        }
    }
}