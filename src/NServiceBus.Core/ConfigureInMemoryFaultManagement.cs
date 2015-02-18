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
        public static void DiscardFailedMessagesInsteadOfSendingToErrorQueue(this BusConfiguration config)
        {
            config.EnableFeature<InMemoryFaultManager>();
            config.DisableFeature<ForwarderFaultManager>();
        }
    }
}
