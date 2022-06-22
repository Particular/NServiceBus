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
        public void Single_outgoing_unicast_operation_matches_spec(bool includeConversationId)
        {
            var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
            var destination = "unicastDestinationAddress";
            var operation = "send";
            var conversationId = "conversation-id";
            var outgoingMessage = CreateMessage();

            if (includeConversationId)
            {
                outgoingMessage.Headers[Headers.ConversationId] = conversationId;
            }

            var destinationTag = new UnicastAddressTag(destination);

            var transportOperations = new[]
            {
                new TransportOperation(outgoingMessage, destinationTag)
            };

            ActivityDecorator.SetOutgoingTraceTags(activity, outgoingMessage, transportOperations);

            VerifyTag(activity, "messaging.message_id", outgoingMessage.MessageId);
            VerifyTag(activity, "messaging.operation", operation);
            VerifyTag(activity, "messaging.destination", destination);
            VerifyTag(activity, "messaging.destination_kind", "queue");
            if (includeConversationId)
            {
                VerifyTag(activity, "messaging.conversation_id", conversationId);
            }
            VerifyTag(activity, "messaging.message_payload_size_bytes", "5");
        }

        [TestCase(true)]
        [TestCase(false)]
        // NOTE: the spec doesn't define what happens here.
        public void Multiple_outgoing_unicast_operations_matches_spec(bool includeConversationId)
        {
            var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
            var destination1 = "unicastDestinationAddress1";
            var destination2 = "unicastDestinationAddress2";
            var destination = $"{destination1}, {destination2}";
            var operation = "send";
            var conversationId = "conversation-id";
            var outgoingMessage = CreateMessage();

            if (includeConversationId)
            {
                outgoingMessage.Headers[Headers.ConversationId] = conversationId;
            }

            var destinationTag1 = new UnicastAddressTag(destination1);
            var destinationTag2 = new UnicastAddressTag(destination2);

            var transportOperations = new[]
            {
                new TransportOperation(outgoingMessage, destinationTag1),
                new TransportOperation(outgoingMessage, destinationTag2),
            };

            ActivityDecorator.SetOutgoingTraceTags(activity, outgoingMessage, transportOperations);

            VerifyTag(activity, "messaging.message_id", outgoingMessage.MessageId);
            VerifyTag(activity, "messaging.operation", operation);
            VerifyTag(activity, "messaging.destination", destination);
            VerifyTag(activity, "messaging.destination_kind", "queue");
            if (includeConversationId)
            {
                VerifyTag(activity, "messaging.conversation_id", conversationId);
            }
            VerifyTag(activity, "messaging.message_payload_size_bytes", "5");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Single_outgoing_multicast_operation_matches_spec(bool includeConversationId)
        {
            var activity = new Activity(ActivityNames.OutgoingEventActivityName);
            var destination = typeof(SomeEvent).FullName;
            var operation = "send";
            var conversationId = "conversation-id";
            var outgoingMessage = CreateMessage();

            if (includeConversationId)
            {
                outgoingMessage.Headers[Headers.ConversationId] = conversationId;
            }

            var destinationTag = new MulticastAddressTag(typeof(SomeEvent));

            var transportOperations = new[]
            {
                new TransportOperation(outgoingMessage, destinationTag)
            };

            ActivityDecorator.SetOutgoingTraceTags(activity, outgoingMessage, transportOperations);

            VerifyTag(activity, "messaging.message_id", outgoingMessage.MessageId);
            VerifyTag(activity, "messaging.operation", operation);
            VerifyTag(activity, "messaging.destination", destination);
            VerifyTag(activity, "messaging.destination_kind", "topic");
            if (includeConversationId)
            {
                VerifyTag(activity, "messaging.conversation_id", conversationId);
            }
            VerifyTag(activity, "messaging.message_payload_size_bytes", "5");
        }

        [TestCase(true)]
        [TestCase(false)]
        // NOTE: the spec doesn't define what happens here.
        // Additionally, core should never do this.
        public void Multiple_outgoing_multicast_operations_matches_spec(bool includeConversationId)
        {
            var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
            var destination1 = typeof(SomeEvent).FullName;
            var destination2 = typeof(SomeOtherEvent).FullName;
            var destination = $"{destination1}, {destination2}";
            var operation = "send";
            var conversationId = "conversation-id";
            var outgoingMessage = CreateMessage();

            if (includeConversationId)
            {
                outgoingMessage.Headers[Headers.ConversationId] = conversationId;
            }

            var destinationTag1 = new MulticastAddressTag(typeof(SomeEvent));
            var destinationTag2 = new MulticastAddressTag(typeof(SomeOtherEvent));

            var transportOperations = new[]
            {
                new TransportOperation(outgoingMessage, destinationTag1),
                new TransportOperation(outgoingMessage, destinationTag2),
            };

            ActivityDecorator.SetOutgoingTraceTags(activity, outgoingMessage, transportOperations);

            VerifyTag(activity, "messaging.message_id", outgoingMessage.MessageId);
            VerifyTag(activity, "messaging.operation", operation);
            VerifyTag(activity, "messaging.destination", destination);
            VerifyTag(activity, "messaging.destination_kind", "topic");
            if (includeConversationId)
            {
                VerifyTag(activity, "messaging.conversation_id", conversationId);
            }
            VerifyTag(activity, "messaging.message_payload_size_bytes", "5");
        }

        [TestCase(true)]
        [TestCase(false)]
        // NOTE: the spec doesn't define what happens here.
        // Additionally, core should never do this.
        public void Multiple_outgoing_mixed_operations_matches_spec(bool includeConversationId)
        {
            var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
            var destination1 = typeof(SomeEvent).FullName;
            var destination2 = "unicastDestination";
            var destination = $"{destination1}, {destination2}";
            var operation = "send";
            var conversationId = "conversation-id";
            var outgoingMessage = CreateMessage();

            if (includeConversationId)
            {
                outgoingMessage.Headers[Headers.ConversationId] = conversationId;
            }

            var destinationTag1 = new MulticastAddressTag(typeof(SomeEvent));
            var destinationTag2 = new UnicastAddressTag(destination2);

            var transportOperations = new[]
            {
                new TransportOperation(outgoingMessage, destinationTag1),
                new TransportOperation(outgoingMessage, destinationTag2),
            };

            ActivityDecorator.SetOutgoingTraceTags(activity, outgoingMessage, transportOperations);

            VerifyTag(activity, "messaging.message_id", outgoingMessage.MessageId);
            VerifyTag(activity, "messaging.operation", operation);
            VerifyTag(activity, "messaging.destination", destination);
            Assert.IsFalse(activity.Tags.Any(x => x.Key == "messaging.destination_kind"), "messaging.destination_kind should not be set");
            if (includeConversationId)
            {
                VerifyTag(activity, "messaging.conversation_id", conversationId);
            }
            VerifyTag(activity, "messaging.message_payload_size_bytes", "5");

        }

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

        class SomeEvent : IEvent
        {

        }

        class SomeOtherEvent : IEvent
        {

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