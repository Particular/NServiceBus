namespace NServiceBus
{
    using Raven.Client;
    using Timeout.Core;
    using Timeout.Hosting.Windows.Config;
    using Timeout.Hosting.Windows.Persistence;


    public static class ConfigureTimeoutManager
    {
        private static bool disabledTimeoutManagerCalledExplicitly;
        private static bool timeoutManagerEnabled;

        public static bool IsTimeoutManagerEnabled(this Configure config)
        {
            return timeoutManagerEnabled;
        }

        public static Configure RunTimeoutManager(this Configure config)
        {
            if(disabledTimeoutManagerCalledExplicitly)
                return config; 
            
            return SetupTimeoutManager(config);
        }

        public static Configure RunTimeoutManagerWithInMemoryPersistence(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.SingleInstance);

            return SetupTimeoutManager(config);
        }

        private static Configure SetupTimeoutManager(this Configure config)
        {
            timeoutManagerEnabled = true;

            TimeoutManagerAddress = config.GetTimeoutManagerAddress();
            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);

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
        /// Sets the default persistence to UseInMemoryTimeoutPersister
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


        /// <summary>
        /// As Timeout manager is turned on by default for server roles, use DisableTimeoutManager method to turn off Timeout manager
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public  static Configure DisableTimeoutManager(this Configure config)
        {
            timeoutManagerEnabled = false;
            disabledTimeoutManagerCalledExplicitly = true;
            return config;
        }

        public static Address TimeoutManagerAddress { get; set; }
    }
}