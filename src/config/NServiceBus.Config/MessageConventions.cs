using System;

namespace NServiceBus
{
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
    }
}
