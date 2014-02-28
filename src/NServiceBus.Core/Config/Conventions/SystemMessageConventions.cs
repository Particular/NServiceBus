namespace NServiceBus.Config.Conventions
{
    using System;

    /// <summary>
    /// Define system message convention
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    public static class SystemMessageConventions
    {
        /// <summary>
        /// Add system messages convention
        /// </summary>
        public static Configure AddSystemMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            MessageConventionExtensions.AddSystemMessagesConventions(definesMessageType);
            return config;
        }
    }
}

namespace NServiceBus
{
    using System;

    /// <summary>
    /// Define system message convention
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    public static class SystemMessageConventions
    {
        /// <summary>
        /// Add system messages convention
        /// </summary>
        public static Configure AddSystemMessagesAs(this Configure config, Func<Type, bool> definesMessageType)
        {
            MessageConventionExtensions.AddSystemMessagesConventions(definesMessageType);
            return config;
        }
    }
}