namespace NServiceBus
{
    using NServiceBus.Features;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static class ConfigureInMemoryFaultManagement
    {
        /// <summary>
        /// Use in-memory fault management.
        /// </summary>
        public static ConfigurationBuilder DiscardFailedMessagesInsteadOfSendingToErrorQueue(this ConfigurationBuilder config)
        {
            return config.EnableFeature<InMemoryFaultManager>();
        }
    }
}
