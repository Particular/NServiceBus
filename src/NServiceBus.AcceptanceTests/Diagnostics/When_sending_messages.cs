﻿namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
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
            // TODO: is this correct? it seems like this needs to be filled by the transport
            var destination = Conventions.EndpointNamingConvention(typeof(TestEndpoint));
            Assert.AreEqual("send", sentMessage.DisplayName);

            var sentMessageTags = sentMessage.Tags.ToImmutableDictionary();
            VerifyTag("nservicebus.message_id", context.SentMessageId);
            VerifyTag("nservicebus.correlation_id", context.SentMessageHeaders[Headers.CorrelationId]);
            VerifyTag("nservicebus.conversation_id", context.SentMessageHeaders[Headers.ConversationId]);
            VerifyTag("nservicebus.content_type", context.SentMessageHeaders[Headers.ContentType]);
            VerifyTag("nservicebus.enclosed_message_types", context.SentMessageHeaders[Headers.EnclosedMessageTypes]);
            VerifyTag("nservicebus.reply_to_address", context.SentMessageHeaders[Headers.ReplyToAddress]);
            VerifyTag("nservicebus.originating_machine", context.SentMessageHeaders[Headers.OriginatingMachine]);
            VerifyTag("nservicebus.originating_endpoint", context.SentMessageHeaders[Headers.OriginatingEndpoint]);
            VerifyTag("nservicebus.version", context.SentMessageHeaders[Headers.NServiceBusVersion]);
            VerifyTag("nservicebus.message_intent", context.SentMessageHeaders[Headers.MessageIntent]);

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
            public IReadOnlyDictionary<string, string> SentMessageHeaders { get; set; }
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
                    testContext.SentMessageHeaders = new ReadOnlyDictionary<string, string>((IDictionary<string, string>)context.MessageHeaders);
                    return Task.CompletedTask;
                }
            }
        }

        public class OutgoingMessage : IMessage
        {
        }
    }
}