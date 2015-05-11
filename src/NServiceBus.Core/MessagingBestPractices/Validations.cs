namespace NServiceBus.MessagingBestPractices
{
    using System;
    using NServiceBus.Logging;

    class Validations
    {
        readonly Conventions conventions;

        public Validations(Conventions conventions)
        {
            this.conventions = conventions;
        }

        public void AssertIsValidForSend(Type messageType)
        {
            if (conventions.IsEventType(messageType) || conventions.IsResponseType(messageType))
            {
                throw new InvalidOperationException("Send is neither supported for Messages, Replies nor Events. Commands should be sent to their logical owner using bus.Send, Replies should be Replied with bus.Reply and Events should be Published with bus.Publish.");
            }
        }

        public void AssertIsValidForReply(Type messageType)
        {
            if (conventions.IsCommandType(messageType) || conventions.IsEventType(messageType))
            {
                throw new InvalidOperationException("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and Events should be Published with bus.Publish.");
            }
        }

        public void AssertIsValidForPubSub(Type messageType)
        {
            if (conventions.IsCommandType(messageType))
            {
                throw new InvalidOperationException("Pub/Sub is not supported for Commands. They should be sent direct to their logical owner.");
            }

            if (conventions.IsResponseType(messageType))
            {
                throw new InvalidOperationException("Pub/Sub is not supported for Responses. They should be replied to their logical owner.");
            }

            if (!conventions.IsEventType(messageType))
            {
                Log.Info("You are using a basic message to do pub/sub, consider implementing the more specific ICommand and IEvent interfaces to help NServiceBus to enforce messaging best practices for you.");
            }
        }

        static ILog Log = LogManager.GetLogger<Validations>();
    }
}
