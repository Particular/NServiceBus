namespace NServiceBus
{
    using Config;
    using Management.Retries;

    public static class ConfigureSecondLevelRetriesExtensions
    {        
        public static Configure DisableSecondLevelRetries(this Configure config)
        {
            SecondLevelRetriesConfiguration.IsDisabled = true;

            //make sure to disable it because satellite will try to bring it up
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