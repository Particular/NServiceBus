namespace NServiceBus.Timeout.Hosting.Azure
{
    using Core;

    public static class ConfigureTimeoutManager
    {
        public static Configure TimeoutManager(this Configure config)
        {
            var configSection = Configure.GetConfigSection<TimeoutManagerConfig>() ?? new TimeoutManagerConfig();

            config.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(tp => tp.ConnectionString, configSection.ConnectionString);

            return config;
        }
    }
}