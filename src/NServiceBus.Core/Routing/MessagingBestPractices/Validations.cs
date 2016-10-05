namespace NServiceBus
{
    using System;
    using Logging;

    class Validations
    {
        public Validations(Conventions conventions)
        {
            this.conventions = conventions;
        }

        public void AssertIsValidForSend(Type messageType)
        {
            if (conventions.IsInSystemConventionList(messageType))
            {
                return;
            }
            if (!conventions.IsEventType(messageType))
            {
                return;
            }
            throw new Exception("Events can have multiple recipient so they should be published.");
        }

        public void AssertIsValidForReply(Type messageType)
        {
            if (conventions.IsInSystemConventionList(messageType))
            {
                return;
            }
            if (!conventions.IsCommandType(messageType) && !conventions.IsEventType(messageType))
            {
                return;
            }
            throw new Exception("Reply is neither supported for Commands nor Events. Commands should be sent to their logical owner. Events should be published.");
        }

        public void AssertIsValidForPubSub(Type messageType)
        {
            if (conventions.IsCommandType(messageType))
            {
                throw new Exception("Pub/Sub is not supported for Commands. They should be be sent direct to their logical owner.");
            }

            if (!conventions.IsEventType(messageType))
            {
                Log.Info("You are using a basic message to do pub/sub, consider implementing the more specific ICommand and IEvent interfaces to help NServiceBus to enforce messaging best practices for you.");
            }
        }

        Conventions conventions;


        static ILog Log = LogManager.GetLogger<Validations>();
    }
}