namespace NServiceBus
{
    using Timeout.Core;
    using Timeout.Hosting.Windows.Persistence;

    public static class ConfigureTimeoutManager
    {
        public static bool TimeoutManagerEnabled { get; private set; }

        public static Configure UseTimeoutManager(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersistence>(DependencyLifecycle.InstancePerCall);

            return SetupTimeoutManager(config);
        }

        public static Configure UseTimeoutManagerWithRavenPersistence(this Configure config)
        {
            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Database, Configure.EndpointName);
            return SetupTimeoutManager(config);
        }

        static Configure SetupTimeoutManager(this Configure config)
        {
            TimeoutManagerEnabled = true;

            TimeoutManagerAddress = Address.Parse(Configure.EndpointName).SubScope("Timeouts");

            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutRunner>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutTransportMessageHandler>(DependencyLifecycle.InstancePerCall);


            return config;
        }

        public static Address TimeoutManagerAddress { get; set; }

    }
}