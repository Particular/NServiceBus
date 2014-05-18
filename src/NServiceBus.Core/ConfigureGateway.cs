namespace NServiceBus
{
    using System;
    using Features;
    using Gateway.Deduplication;
    using Gateway.Persistence;
    using Gateway.Persistence.Raven;
    using Persistence.Raven;

    [ObsoleteEx]
    public static class ConfigureGateway
    {
        /// <summary>
        /// The Gateway is turned on by default for the Master role. Call DisableGateway method to turn the Gateway off.
        /// </summary>
        public static Configure DisableGateway(this Configure config)
        {
            Feature.Disable<Features.Gateway>();
            return config;
        }

        /// <summary>
        /// Configuring to run the Gateway. By default Gateway will use RavenPersistence (see GatewayDefaults class).
        /// </summary>
        public static Configure RunGateway(this Configure config)
        {
            Feature.Enable<Features.Gateway>();

            return config;
        }

        public static Configure RunGatewayWithInMemoryPersistence(this Configure config)
        {
            return RunGateway(config, typeof(InMemoryGatewayPersister));
        }

        public static Configure RunGatewayWithRavenPersistence(this Configure config)
        {
            return RunGateway(config, typeof(RavenDbPersistence));
        }

        public static Configure RunGateway(this Configure config, Type persistence)
        {
            config.Configurer.ConfigureComponent(persistence, DependencyLifecycle.SingleInstance);
            Feature.Enable<Features.Gateway>();
            return config;
        }

        /// <summary>
        /// Use the in memory messages persistence by the gateway.
        /// </summary>
        public static Configure UseInMemoryGatewayPersister(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryGatewayPersister>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Use in-memory message deduplication for the gateway.
        /// </summary>
        public static Configure UseInMemoryGatewayDeduplication(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);
            return config;
        }


        /// <summary>
        /// Use RavenDB messages persistence by the gateway.
        /// </summary>
        public static Configure UseRavenGatewayPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<StoreAccessor>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenDbPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Use RavenDB for message deduplication by the gateway.
        /// </summary>
        public static Configure UseRavenGatewayDeduplication(this Configure config)
        {
            if (!config.Configurer.HasComponent<StoreAccessor>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenDBDeduplication>(DependencyLifecycle.SingleInstance);
            return config;
        }
    }
}