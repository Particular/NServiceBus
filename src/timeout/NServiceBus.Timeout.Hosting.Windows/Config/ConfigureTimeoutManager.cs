namespace NServiceBus
{
    using Timeout.Core;
    using Timeout.Hosting.Windows.Persistence;


    public static class ConfigureTimeoutManager
    {
        public static bool TimeoutManagerEnabled { get; private set; }

        public static Configure UseTimeoutManager(this Configure config)
        {
            return SetupTimeoutManager(config);
        }

        public static Configure UseTimeoutManagerWithInMemoryPersistence(this Configure config)
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

        public static Address TimeoutManagerAddress { get; set; }

    }
}