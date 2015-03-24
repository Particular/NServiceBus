namespace NServiceBus
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Conventions builder class.
    /// </summary>
    public class ConventionsBuilder
    {
        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a message.
        /// </summary>
        public ConventionsBuilder DefiningMessagesAs(Func<Type, bool> definesMessageType)
        {
            Guard.AgainstNull(definesMessageType, "definesMessageType");
            Conventions.IsMessageTypeAction = definesMessageType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        public ConventionsBuilder DefiningCommandsAs(Func<Type, bool> definesCommandType)
        {
            Guard.AgainstNull(definesCommandType, "definesCommandType");
            Conventions.IsCommandTypeAction = definesCommandType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        public ConventionsBuilder DefiningEventsAs(Func<Type, bool> definesEventType)
        {
            Guard.AgainstNull(definesEventType, "definesEventType");
            Conventions.IsEventTypeAction = definesEventType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a property should be encrypted or not.
        /// </summary>
        public ConventionsBuilder DefiningEncryptedPropertiesAs(Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            Guard.AgainstNull(definesEncryptedProperty, "definesEncryptedProperty");
            Conventions.IsEncryptedPropertyAction = definesEncryptedProperty;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
        /// </summary>
        public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty)
        {
            Guard.AgainstNull(definesDataBusProperty, "definesDataBusProperty");
            Conventions.IsDataBusPropertyAction = definesDataBusProperty;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        public ConventionsBuilder DefiningTimeToBeReceivedAs(Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            Guard.AgainstNull(retrieveTimeToBeReceived, "retrieveTimeToBeReceived");
            Conventions.TimeToBeReceivedAction = retrieveTimeToBeReceived;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        public ConventionsBuilder DefiningExpressMessagesAs(Func<Type, bool> definesExpressMessageType)
        {
            Guard.AgainstNull(definesExpressMessageType, "definesExpressMessageType");
            Conventions.IsExpressMessageAction = definesExpressMessageType;
            return this;
        }

        internal Conventions Conventions = new Conventions();
    }
}