namespace NServiceBus
{
    using Raven.Client;
    using Timeout.Core;
    using Timeout.Hosting.Windows.Config;
    using Timeout.Hosting.Windows.Persistence;


    public static class ConfigureTimeoutManager
    {
        public static bool TimeoutManagerEnabled { get; private set; }

        public static bool IsTimeoutManagerEnabled(this Configure config)
        {
            return TimeoutManagerEnabled;
        }

        public static Configure RunTimeoutManager(this Configure config)
        {
            return SetupTimeoutManager(config);
        }

        public static Configure RunTimeoutManagerWithInMemoryPersistence(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return SetupTimeoutManager(config);
        }

        static Configure SetupTimeoutManager(this Configure config)
        {
            TimeoutManagerEnabled = true;

            TimeoutManagerAddress = config.GetTimeoutManagerAddress();

            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutRunner>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutTransportMessageHandler>(DependencyLifecycle.InstancePerCall);

            return config;
        }

        /// <summary>
        /// Use the in memory timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Sets the default persitence to inmemory
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DefaultToInMemoryTimeoutPersistence(this Configure config)
        {
            TimeoutManagerDefaults.DefaultPersistence = () => UseInMemoryTimeoutPersister(config);
            return config;
        }

        /// <summary>
        /// Use the Raven timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseRavenTimeoutPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();
                
            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return config;
        }


        public static Address TimeoutManagerAddress { get; set; }

    }
}