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
            throw new Exception($"Best practice violation for message type '{messageType.FullName}'. Events can have multiple recipients, so they should be published.");
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
            throw new Exception($"Best practice violation for message type '{messageType.FullName}'. Reply is not supported for commands or events. Commands should be sent to their logical owner. Events should be published.");
        }

        public void AssertIsValidForPubSub(Type messageType)
        {
            if (conventions.IsCommandType(messageType))
            {
                throw new Exception($"Best practice violation for message type '{messageType.FullName}'. Pub/sub is not supported for commands, so they should be be sent to their logical owner instead.");
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
