namespace NServiceBus.Config.Conventions
{
    using System;

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
        public static Configure AddSystemMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            MessageConventionExtensions.AddSystemMessagesConventions(definesMessageType);
            return config;
        }
    }
}
