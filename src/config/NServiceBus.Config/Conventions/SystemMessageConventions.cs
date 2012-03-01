using System;

namespace NServiceBus.Config.Conventions
{
    /// <summary>
    /// Define system message convention
    /// </summary>
    public static class SystemMessageConventions
    {
        /// <summary>
        /// Add system messages convention
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesMessageType"></param>
        public static Configure DefiningSystemMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            MessageConventionExtensions.AddSystemMessagesConventions(definesMessageType);
            return config;
        }
    }
}
