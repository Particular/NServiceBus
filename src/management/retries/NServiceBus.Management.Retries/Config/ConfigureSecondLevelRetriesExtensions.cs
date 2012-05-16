using NServiceBus.Management.Retries;

namespace NServiceBus
{
    public static class ConfigureSecondLevelRetriesExtensions
    {        
        public static Configure DisableSecondLevelRetries(this Configure config)
        {
            config.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(rs => rs.Disabled, true);

            return config;
        }
    }
}