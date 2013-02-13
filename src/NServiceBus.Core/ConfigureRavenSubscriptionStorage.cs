namespace NServiceBus
{
    using Persistence.Raven;
    using Raven.Client;
    using Unicast.Subscriptions.Raven;

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