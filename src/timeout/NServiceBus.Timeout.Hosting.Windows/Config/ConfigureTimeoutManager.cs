namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using Core;
    using ObjectBuilder;
    using Persistence;

    public static class ConfigureTimeoutManager
    {
        public static bool TimeoutManagerEnabled { get; private set; }

        public static Configure UseTimeoutManager(this Configure config)
        {
            TimeoutManagerEnabled = true;

            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutMessageHandler>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<RavenTimeoutPersistence>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p=>p.Database, Configure.EndpointName);

            return config;
        }
    }
}