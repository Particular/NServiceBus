namespace NServiceBus
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Static extension methods to Configure.
    /// </summary>
    public static class MessageConventions
    {
        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a message.
        /// </summary>
        public static Configure DefiningMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            MessageConventionExtensions.IsMessageTypeAction = definesMessageType;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        public static Configure DefiningCommandsAs(this Configure config, Func<Type, bool> definesCommandType)
        {
            MessageConventionExtensions.IsCommandTypeAction = definesCommandType;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        public static Configure DefiningEventsAs(this Configure config, Func<Type, bool> definesEventType)
        {
            MessageConventionExtensions.IsEventTypeAction = definesEventType;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a property should be encrypted or not.
        /// </summary>
        public static Configure DefiningEncryptedPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            MessageConventionExtensions.IsEncryptedPropertyAction = definesEncryptedProperty;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
        /// </summary>
        public static Configure DefiningDataBusPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesDataBusProperty)
        {
            MessageConventionExtensions.IsDataBusPropertyAction = definesDataBusProperty;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        public static Configure DefiningTimeToBeReceivedAs(this Configure config, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            MessageConventionExtensions.TimeToBeReceivedAction = retrieveTimeToBeReceived;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        public static Configure DefiningExpressMessagesAs(this Configure config, Func<Type, bool> definesExpressMessageType)
        {
            MessageConventionExtensions.IsExpressMessageAction = definesExpressMessageType;
            return config;
        }
    }
}
