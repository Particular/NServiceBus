﻿namespace NServiceBus.AcceptanceTests.Diagnostics
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
            Assert.AreEqual("process message", incomingActivity.DisplayName);

            var incomingActivityTags = incomingActivity.Tags.ToImmutableDictionary();

            incomingActivityTags.VerifyTag("messaging.message_id", context.IncomingMessageId);
            incomingActivityTags.VerifyTag("messaging.conversation_id", context.IncomingMessageConversationId);
            incomingActivityTags.VerifyTag("messaging.operation", "process");
            incomingActivityTags.VerifyTag("messaging.destination", destination);
            incomingActivityTags.VerifyTag("messaging.message_payload_size_bytes", "222");

            incomingActivityTags.VerifyTag("nservicebus.message_id", context.IncomingMessageId);
            incomingActivityTags.VerifyTag("nservicebus.correlation_id", context.ReceivedHeaders[Headers.CorrelationId]);
            incomingActivityTags.VerifyTag("nservicebus.conversation_id", context.ReceivedHeaders[Headers.ConversationId]);
            incomingActivityTags.VerifyTag("nservicebus.content_type", context.ReceivedHeaders[Headers.ContentType]);
            incomingActivityTags.VerifyTag("nservicebus.enclosed_message_types", context.ReceivedHeaders[Headers.EnclosedMessageTypes]);
            incomingActivityTags.VerifyTag("nservicebus.reply_to_address", context.ReceivedHeaders[Headers.ReplyToAddress]);
            incomingActivityTags.VerifyTag("nservicebus.originating_machine", context.ReceivedHeaders[Headers.OriginatingMachine]);
            incomingActivityTags.VerifyTag("nservicebus.originating_endpoint", context.ReceivedHeaders[Headers.OriginatingEndpoint]);
            incomingActivityTags.VerifyTag("nservicebus.version", context.ReceivedHeaders[Headers.NServiceBusVersion]);
            incomingActivityTags.VerifyTag("nservicebus.message_intent", context.ReceivedHeaders[Headers.MessageIntent]);

            incomingActivityTags.VerifyTag("nservicebus.native_message_id", context.IncomingNativeMessageId);

            incomingActivity.VerifyUniqueTags();
        }

        class Context : ScenarioContext
        {
            public string IncomingMessageId { get; set; }
            public string IncomingMessageConversationId { get; set; }
            public bool IncomingMessageReceived { get; set; }
            public IReadOnlyDictionary<string, string> ReceivedHeaders { get; set; }
            public string IncomingNativeMessageId { get; set; }
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
                    testContext.IncomingNativeMessageId = context.Extensions.Get<NServiceBus.Transport.IncomingMessage>().NativeMessageId;
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