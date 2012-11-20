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
        /// <param name="config"></param>
        /// <param name="definesMessageType"></param>
        public static Configure DefiningMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            MessageConventionExtensions.IsMessageTypeAction = definesMessageType;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesCommandType"></param>
        public static Configure DefiningCommandsAs(this Configure config, Func<Type, bool> definesCommandType)
        {
            MessageConventionExtensions.IsCommandTypeAction = definesCommandType;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesEventType"></param>
        public static Configure DefiningEventsAs(this Configure config, Func<Type, bool> definesEventType)
        {
            MessageConventionExtensions.IsEventTypeAction = definesEventType;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a property should be encrypted or not.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesEncryptedProperty"></param>
        public static Configure DefiningEncryptedPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesEncryptedProperty)
        {
            MessageConventionExtensions.IsEncryptedPropertyAction = definesEncryptedProperty;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesDataBusProperty"></param>
        public static Configure DefiningDataBusPropertiesAs(this Configure config, Func<PropertyInfo, bool> definesDataBusProperty)
        {
            MessageConventionExtensions.IsDataBusPropertyAction = definesDataBusProperty;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a message has a time to be received.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="retrieveTimeToBeReceived"></param>
        public static Configure DefiningTimeToBeReceivedAs(this Configure config, Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            MessageConventionExtensions.TimeToBeReceivedAction = retrieveTimeToBeReceived;
            return config;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is an express message or not.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesExpressMessageType"></param>
        public static Configure DefiningExpressMessagesAs(this Configure config, Func<Type, bool> definesExpressMessageType)
        {
            MessageConventionExtensions.IsExpressMessageAction = definesExpressMessageType;
            return config;
        }
    }
}
