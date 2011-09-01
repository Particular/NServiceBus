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
    }
}
