namespace NServiceBus
{
    using Features;
    using Persistence.InMemory.TimeoutPersister;
    using Persistence.Raven;
    using Persistence.Raven.TimeoutPersister;

    public static class ConfigureTimeoutManager
    {
        /// <summary>
        /// Use the in memory timeout persister implementation.
        /// </summary>
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Use the Raven timeout persister implementation.
        /// </summary>
        public static Configure UseRavenTimeoutPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<StoreAccessor>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return config;
        }

   
        /// <summary>
        /// As Timeout manager is turned on by default for server roles, use DisableTimeoutManager method to turn off Timeout manager
        /// </summary>
        public static Configure DisableTimeoutManager(this Configure config)
        {
            Feature.Disable<TimeoutManager>();
            return config;
        }

    }
}