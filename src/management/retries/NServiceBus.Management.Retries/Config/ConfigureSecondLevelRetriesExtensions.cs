using NServiceBus.Management.Retries;

namespace NServiceBus
{
    public static class ConfigureSecondLevelRetriesExtensions
    {        
        public static Configure DisableSecondLevelRetries(this Configure config)
        {
            if (config.Configurer.HasComponent<SecondLevelRetries>())
            {
                config.Configurer.ConfigureProperty<SecondLevelRetries>(p => p.Disabled, true);
            }
            else
            {
                config.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(p => p.Disabled, true);
            }
            return config;
        }
    }
}