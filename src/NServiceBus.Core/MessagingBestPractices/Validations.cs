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
            if (conventions.IsEventType(messageType))
            {
                throw new InvalidOperationException("Events can have multiple recipient so they should be published");
            }
        }

        public void AssertIsValidForReply(Type messageType)
        {
            if (conventions.IsCommandType(messageType) || conventions.IsEventType(messageType))
            {
                throw new InvalidOperationException("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner using bus.Send and bus. Events should be Published with bus.Publish.");
            }
        }

        public void AssertIsValidForPubSub(Type messageType)
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

        static ILog Log = LogManager.GetLogger<Validations>();
    }
}
