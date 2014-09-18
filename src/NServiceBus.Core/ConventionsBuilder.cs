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
            Conventions.IsMessageTypeAction = definesMessageType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        public ConventionsBuilder DefiningCommandsAs(Func<Type, bool> definesCommandType)
        {
            Conventions.IsCommandTypeAction = definesCommandType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        public ConventionsBuilder DefiningEventsAs(Func<Type, bool> definesEventType)
        {
            Conventions.IsEventTypeAction = definesEventType;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a property should be encrypted or not.
        /// </summary>
        public ConventionsBuilder DefiningEncryptedPropertiesAs(Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            Conventions.IsEncryptedPropertyAction = definesEncryptedProperty;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
        /// </summary>
        public ConventionsBuilder DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty)
        {
            Conventions.IsDataBusPropertyAction = definesDataBusProperty;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        public ConventionsBuilder DefiningTimeToBeReceivedAs(Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            Conventions.TimeToBeReceivedAction = retrieveTimeToBeReceived;
            return this;
        }

        /// <summary>
        ///     Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        public ConventionsBuilder DefiningExpressMessagesAs(Func<Type, bool> definesExpressMessageType)
        {
            Conventions.IsExpressMessageAction = definesExpressMessageType;
            return this;
        }

        internal Conventions Conventions = new Conventions();
    }
}