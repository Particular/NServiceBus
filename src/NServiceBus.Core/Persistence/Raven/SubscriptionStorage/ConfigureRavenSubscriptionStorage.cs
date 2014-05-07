namespace NServiceBus
{
    using System;

    [ObsoleteEx]
    public static class ConfigureRavenSubscriptionStorage
    {
        public static Configure RavenSubscriptionStorage(this Configure config)
        {
//            if (!config.Configurer.HasComponent<StoreAccessor>())
//                config.RavenPersistence();
//
//            config.Configurer.ConfigureComponent<RavenSubscriptionStorage>(DependencyLifecycle.SingleInstance);
//
//            return config;
            throw new NotImplementedException("RavenDB usage has been moved out of the core");
        }
    }
}