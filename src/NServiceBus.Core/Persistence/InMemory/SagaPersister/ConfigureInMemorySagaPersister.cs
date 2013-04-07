namespace NServiceBus
{
    using Persistence.InMemory.SagaPersister;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the in memory saga persister.
    /// </summary>
    public static class ConfigureInMemorySagaPersister
    {
        /// <summary>
        /// Use the in memory saga persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure InMemorySagaPersister(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemorySagaPersister>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}
