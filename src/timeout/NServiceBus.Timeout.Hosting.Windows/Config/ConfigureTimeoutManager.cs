namespace NServiceBus
{
    using Raven.Client;
    using Timeout.Core;
    using Timeout.Hosting.Windows;
    using Timeout.Hosting.Windows.Persistence;


    public static class ConfigureTimeoutManager
    {
        /// <summary>
        /// Use the in memory timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            SetupTimeoutManager(config);

            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Use the Raven timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseRavenTimeoutPersister(this Configure config)
        {
            SetupTimeoutManager(config);

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
        [ObsoleteEx(Message = "As Timeout manager is a core functionality of NServiceBus it will be impossible to disable it beginning version 4.0.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]        
        public static Configure DisableTimeoutManager(this Configure config)
        {
            return config;
        }

        public static Address TimeoutManagerAddress { get; set; }

        private static void SetupTimeoutManager(Configure config)
        {
            TimeoutManagerAddress = config.GetTimeoutManagerAddress();

            config.Configurer.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
        }
    }
}