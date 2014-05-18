namespace NServiceBus
{
    using InMemory.SagaPersister;
    using Persistence;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the in memory saga persister.
    /// </summary>
    public static class ConfigureInMemorySagaPersister
    {
        /// <summary>
        /// Use the in memory saga persister implementation.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
        public static Configure InMemorySagaPersister(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemorySagaPersister>(DependencyLifecycle.SingleInstance);

            return config.UsePersistence<Persistence.InMemory>();
        }
    }
}
