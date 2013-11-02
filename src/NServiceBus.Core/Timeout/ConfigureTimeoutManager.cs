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


        [ObsoleteEx(Message = "As Timeout Manager is part of the core NServiceBus functionality, it is not required to call this method any longer.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure RunTimeoutManager(this Configure config)
        {
            Feature.Enable<TimeoutManager>();
            return config;
        }

        /// <summary>
        /// Sets the default persistence to InMemory.
        /// </summary>
        [ObsoleteEx(Replacement = "UseInMemoryTimeoutPersister()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure RunTimeoutManagerWithInMemoryPersistence(this Configure config)
        {
            Feature.Enable<TimeoutManager>();

            return UseInMemoryTimeoutPersister(config);
        }

        /// <summary>
        /// Sets the default persistence to InMemory.
        /// </summary>
        [ObsoleteEx(Replacement = "UseInMemoryTimeoutPersister()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DefaultToInMemoryTimeoutPersistence(this Configure config)
        {
            return config;
        }
    }
}