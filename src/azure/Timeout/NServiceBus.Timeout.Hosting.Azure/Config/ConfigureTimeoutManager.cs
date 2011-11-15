using NServiceBus.ObjectBuilder;
using Timeout.MessageHandlers;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public static class ConfigureTimeoutManager
    {
        public static Configure TimeoutManager(this Configure config)
        {
            var configSection = Configure.GetConfigSection<TimeoutManagerConfig>() ?? new TimeoutManagerConfig();

            config.Configurer.ConfigureComponent<TimeoutManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(tp => tp.ConnectionString, configSection.ConnectionString);

            return config;
        }
    }
}