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
            this.definesMessageType = definesMessageType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        public ConventionsBuilder DefiningCommandsAs(Func<Type, bool> definesCommandType)
        {
            this.definesCommandType = definesCommandType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        public ConventionsBuilder DefiningEventsAs(Func<Type, bool> definesEventType)
        {
            this.definesEventType = definesEventType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a property should be encrypted or not.
        /// </summary>
        public ConventionsBuilder DefiningEncryptedPropertiesAs(Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            this.definesEncryptedProperty = definesEncryptedProperty;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
        /// </summary>
        public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty)
        {
            this.definesDataBusProperty = definesDataBusProperty;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        public ConventionsBuilder DefiningTimeToBeReceivedAs(Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            this.retrieveTimeToBeReceived = retrieveTimeToBeReceived;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        public ConventionsBuilder DefiningExpressMessagesAs(Func<Type, bool> definesExpressMessageType)
        {
            this.definesExpressMessageType = definesExpressMessageType;
            return this;
        }

        internal Conventions BuildConventions()
        {
            return new Conventions(isCommandTypeAction: definesCommandType, isDataBusPropertyAction: definesDataBusProperty, isEncryptedPropertyAction: definesEncryptedProperty, isEventTypeAction: definesEventType, isExpressMessageAction: definesExpressMessageType, isMessageTypeAction: definesMessageType, timeToBeReceivedAction: retrieveTimeToBeReceived);
        }

        Func<Type, bool> definesCommandType;
        Func<PropertyInfo, bool> definesDataBusProperty;
        Func<PropertyInfo, bool> definesEncryptedProperty;
        Func<Type, bool> definesEventType;
        Func<Type, bool> definesExpressMessageType;
        Func<Type, bool> definesMessageType;
        Func<Type, TimeSpan> retrieveTimeToBeReceived;
    }
}