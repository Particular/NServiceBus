namespace NServiceBus
{
    using Faults.InMemory;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static class ConfigureInMemoryFaultManagement
    {
        /// <summary>
        /// Use in-memory fault management.
        /// </summary>
        public static Configure InMemoryFaultManagement(this Configure config)
        {
            config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}
