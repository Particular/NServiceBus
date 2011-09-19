using System;
using NServiceBus.Config;

namespace NServiceBus
{
    /// <summary>
    /// Static extension methods to Configure.
    /// </summary>
    public static class MessageType
    {
        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a message.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesMessageType"></param>
        public static void DefiningMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            ExtensionMethods.IsMessageTypeAction = definesMessageType;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a commands.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesCommandType"></param>
        public static void DefiningCommandsAs(this Configure config, Func<Type, bool> definesCommandType)
        {
            ExtensionMethods.IsCommandTypeAction = definesCommandType;
        }

        /// <summary>
        /// Sets the function to be used to evaluate whether a type is a event.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesEventType"></param>
        public static void DefiningEventsAs(this Configure config, Func<Type, bool> definesEventType)
        {
            ExtensionMethods.IsCommandTypeAction = definesEventType;
        }
    }
}
