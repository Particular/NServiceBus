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


        public static Address TimeoutManagerAddress { get; set; }

        private static void SetupTimeoutManager(Configure config)
        {
            TimeoutManagerAddress = config.GetTimeoutManagerAddress();

            config.Configurer.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
        }

        /// <summary>
        /// As Timeout manager is turned on by default for server roles, use DisableTimeoutManager method to turn off Timeout manager
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DisableTimeoutManager(this Configure config)
        {
            TimeoutManager.Enabled = false;
            return config;
        }

        public static bool IsTimeoutManagerEnabled(this Configure config)
        {
            return TimeoutManager.Enabled;
        }

        [ObsoleteEx(Message = "As Timeout Manager is part of the core NServiceBus functionality, it is not required to call this method any longer.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure RunTimeoutManager(this Configure config)
        {
            return config;
        }

        /// <summary>
        /// Sets the default persistence to InMemory.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(Replacement = "UseInMemoryTimeoutPersister()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure RunTimeoutManagerWithInMemoryPersistence(this Configure config)
        {
            return config;
        }

        /// <summary>
        /// Sets the default persistence to InMemory.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(Replacement = "UseInMemoryTimeoutPersister()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DefaultToInMemoryTimeoutPersistence(this Configure config)
        {
            return config;
        }
    }

    public class TimeoutManager
    {
        static TimeoutManager()
        {
            Enabled = true;
        }

        public static bool Enabled { get; set; }
    }
}