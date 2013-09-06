namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using Logging;

    /// <summary>
    /// Enforce messaging rules
    /// </summary>
    public class MessagingBestPractices
    {
        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used within the <see cref="IBus.Send(object[])"/>.
        /// </summary>
        /// <param name="messageType">Event, Command or message</param>
        /// <param name="messageIntent"></param>
        public static void AssertIsValidForSend(Type messageType, MessageIntentEnum messageIntent)
        {
            if (MessageConventionExtensions.IsEventType(messageType) && messageIntent != MessageIntentEnum.Publish)
            {
                throw new InvalidOperationException("Events can have multiple recipient so they should be published");
            }
        }

        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used by <see cref="IBus.Reply"/>.
        /// </summary>
        /// <param name="messages">Collection of messages to enforce messaging rules on.</param>
        public static void AssertIsValidForReply(IEnumerable<object> messages)
        {
            foreach (var message in messages)
            {
                if (MessageConventionExtensions.IsCommand(message)) 
                {
                    throw new InvalidOperationException("Reply is not supported for Commands. Commands should be sent to their logical owner using bus.Send and bus.");
                }
                if (MessageConventionExtensions.IsEvent(message)) 
                {
                    throw new InvalidOperationException("Reply is not supported for Events. Events should be Published with bus.Publish.");
                }
            }
        }
        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used by <see cref="IBus.Reply"/>.
        /// </summary>
        /// <param name="messageType"></param>
        [ObsoleteEx(RemoveInVersion = "6.0",TreatAsErrorFromVersion = "5.0")]
        public static void AssertIsValidForReply(Type messageType)
        {
            if (MessageConventionExtensions.IsCommandType(messageType) || MessageConventionExtensions.IsEventType(messageType))
            {
                throw new InvalidOperationException("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and bus. Events should be Published with bus.Publish.");
            }
        }
        /// <summary>
        /// Enforce messaging rules. Make sure, the message can be used by pubsub bus methods (<see cref="IBus.Subscribe(System.Type)"/>, <see cref="IBus.Unsubscribe"/> and <see cref="IBus.Publish{T}(T[])"/>)..
        /// </summary>
        /// <param name="messageType"></param>
        public static void AssertIsValidForPubSub(Type messageType)
        {
            if (MessageConventionExtensions.IsCommandType(messageType))
            {
                throw new InvalidOperationException("Pub/Sub is not supported for Commands. They should be be sent direct to their logical owner.");
            }

            if (!MessageConventionExtensions.IsEventType(messageType))
            {
                Log.Info("You are using a basic message to do pub/sub, consider implementing the more specific ICommand and IEvent interfaces to help NServiceBus to enforce messaging best practices for you.");
            }
        }

        static ILog Log = LogManager.GetLogger(typeof(MessagingBestPractices));
    }
}
