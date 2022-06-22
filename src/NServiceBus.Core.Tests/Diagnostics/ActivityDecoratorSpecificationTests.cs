namespace NServiceBus.Core.Tests.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class ActivityDecoratorSpecificationTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Incoming_operation_matches_spec(bool includeConversationId)
        {
            var activity = new Activity(ActivityNames.IncomingMessageActivityName);
            var destination = "this-endpoint-address";
            ActivityDecorator.Initialize(destination);
            var operation = "process";
            var nativeMessageId = "native-message-id";
            var nservicebusMessageId = "nservicebus-message-id";
            var conversationId = "conversation-id";
            var headers = new Dictionary<string, string>
            {
                [Headers.MessageId] = nservicebusMessageId
            };
            if (includeConversationId)
            {
                headers[Headers.ConversationId] = conversationId;
            }
            var body = new ReadOnlyMemory<byte>(new byte[5]);

            var incomingMessage = new IncomingMessage(nativeMessageId, headers, body);

            ActivityDecorator.SetReceiveTags(activity, incomingMessage);

            VerifyTag(activity, "messaging.message_id", incomingMessage.MessageId);
            VerifyTag(activity, "messaging.operation", operation);
            VerifyTag(activity, "messaging.destination", destination);
            VerifyTag(activity, "messaging.message_payload_size_bytes", "5");
            if (includeConversationId)
            {
                VerifyTag(activity, "messaging.conversation_id", conversationId);
            }
        }

        static OutgoingMessage CreateMessage()
        {
            var messageId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>();
            var body = new ReadOnlyMemory<byte>(new byte[5]);
            return new OutgoingMessage(messageId, headers, body);
        }

        static void VerifyTag(Activity activity, string tagName, string expectedValue)
        {
            var tags = activity.Tags.ToImmutableDictionary();
            Assert.IsTrue(tags.TryGetValue(tagName, out var actualValue), $"Activity should have a tag named {tagName}");
            Assert.AreEqual(expectedValue, actualValue, $"Incorrect tag. Expected {expectedValue}. Actual {actualValue}");
        }
    }
}