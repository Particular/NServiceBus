namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    /// Enforce messaging rules
    /// </summary>
    public class MessagingBestPractices
    {
        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used within the Bus.Send.
        /// </summary>
        /// <param name="messageType">Event, Command or message</param>
        /// <param name="messageIntent"></param>
        public static void AssertIsValidForSend(Type messageType, MessageIntentEnum messageIntent)
        {
            if (MessageConventionExtensions.IsEventType(messageType) && messageIntent != MessageIntentEnum.Publish)
                throw new InvalidOperationException(
                    "Events can have multiple recipient so they should be published");
        }

        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used by Bus.Reply.
        /// </summary>
        /// <param name="messages">Collection of messages to enforce messaging rules on.</param>
        public static void AssertIsValidForReply(IEnumerable<object> messages)
        {
            if (messages.Any(m => MessageConventionExtensions.IsCommand(m) || MessageConventionExtensions.IsEvent(m)))
                throw new InvalidOperationException(
                    "Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and bus. Events should be Published with bus.Publish.");
        }
        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used by Bus.Reply.
        /// </summary>
        /// <param name="messageType"></param>
        public static void AssertIsValidForReply(Type messageType)
        {
            if (MessageConventionExtensions.IsCommandType(messageType) || MessageConventionExtensions.IsEventType(messageType))
                throw new InvalidOperationException(
                    "Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and bus. Events should be Published with bus.Publish.");
        }
        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used by pubsub bus methods (subscribe, unsubscribe and publish)..
        /// </summary>
        /// <param name="messageType"></param>
        public static void AssertIsValidForPubSub(Type messageType)
        {
            if (MessageConventionExtensions.IsCommandType(messageType))
                throw new InvalidOperationException(
                    "Pub/Sub is not supported for Commands. They should be be sent direct to their logical owner");

            if (!MessageConventionExtensions.IsEventType(messageType))
                Log.Info("You are using a basic message to do pub/sub, consider implementing the more specific ICommand and IEvent interfaces to help NServiceBus to enforce messaging best practices for you");
        }

        private readonly static ILog Log = LogManager.GetLogger(typeof(MessagingBestPractices));
    }
}
