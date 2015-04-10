namespace NServiceBus.Unicast
{
    using System;
    using Logging;

    class MessagingBestPractices
    {
        public static void AssertIsValidForSend(Type messageType, Conventions conventions)
        {
            if (conventions.IsEventType(messageType))
            {
                throw new InvalidOperationException("Events can have multiple recipient so they should be published");
            }
        }

        public static void AssertIsValidForReply(Type messageType, Conventions conventions)
        {
            if (conventions.IsCommandType(messageType) || conventions.IsEventType(messageType))
            {
                throw new InvalidOperationException("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and bus. Events should be Published with bus.Publish.");
            }
        }

        public static void AssertIsValidForPubSub(Type messageType, Conventions conventions)
        {
            if (conventions.IsCommandType(messageType))
            {
                throw new InvalidOperationException("Pub/Sub is not supported for Commands. They should be be sent direct to their logical owner.");
            }

            if (!conventions.IsEventType(messageType))
            {
                Log.Info("You are using a basic message to do pub/sub, consider implementing the more specific ICommand and IEvent interfaces to help NServiceBus to enforce messaging best practices for you.");
            }
        }

        public static void AssertIsValidForDefer(Type messageType, Conventions conventions)
        {
            if (conventions.GetTimeToBeReceived(messageType) < TimeSpan.MaxValue)
            {
                throw new InvalidOperationException("Defering messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to defer messages of this type.");
            }
        }

        static ILog Log = LogManager.GetLogger<MessagingBestPractices>();
    }
}
