namespace NServiceBus
{
    using NServiceBus.Faults.Forwarder;
    using NServiceBus.Features;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static partial class ConfigureInMemoryFaultManagement
    {
        /// <summary>
        /// Use in-memory fault management.
        /// </summary>
        public static void DiscardFailedMessagesInsteadOfSendingToErrorQueue(this ConfigurationBuilder config)
        {
            config.EnableFeature<InMemoryFaultManager>();
            config.DisableFeature<ForwarderFaultManager>();
        }
    }
}
