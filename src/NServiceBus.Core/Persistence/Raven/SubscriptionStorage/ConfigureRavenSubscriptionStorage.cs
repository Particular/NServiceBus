namespace NServiceBus
{
    using Persistence.Raven;
    using Persistence.Raven.SubscriptionStorage;

    [ObsoleteEx]
    public static class ConfigureRavenSubscriptionStorage
    {
        public static Configure RavenSubscriptionStorage(this Configure config)
        {
            if (!config.Configurer.HasComponent<StoreAccessor>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenSubscriptionStorage>(DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}