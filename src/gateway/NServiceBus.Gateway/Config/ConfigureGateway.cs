namespace NServiceBus
{
    using System;
    using System.Linq;
    using Config;
    using Gateway.Channels;
    using Gateway.Config;
    using Gateway.Notifications;
    using Gateway.Persistence;
    using Gateway.Persistence.Raven;
    using Gateway.Receiving;
    using Gateway.Routing.Endpoints;
    using Gateway.Routing.Sites;
    using Gateway.Sending;
    using Raven.Client;

    public static class ConfigureGateway
    {
        public static Address GatewayInputAddress { get; private set; }

        /// <summary>
        /// Configuring to run the Gateway. By default Gateway will use RavenPersistence (see GatewayDefaults class).
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RunGateway(this Configure config)
        {
            return SetupGateway(config);
        }

        public static Configure RunGatewayWithInMemoryPersistence(this Configure config)
        {
            return RunGateway(config, typeof(InMemoryPersistence));
        }
        public static Configure RunGatewayWithRavenPersistence(this Configure config)
        {
            return RunGateway(config, typeof(RavenDbPersistence));
        }
        public static Configure RunGateway(this Configure config, Type persistence)
        {
            config.Configurer.ConfigureComponent(persistence, DependencyLifecycle.SingleInstance);

            return SetupGateway(config);
        }

        /// <summary>
        /// Use the in memory messages persistence by the gateway.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseInMemoryGatewayPersister(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        /// <summary>
        /// Sets the default persistence to inmemory
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DefaultToInMemoryGatewayPersistence(this Configure config)
        {
            GatewayDefaults.DefaultPersistence = () => UseInMemoryGatewayPersister(config);

            return config;
        }
        /// <summary>
        /// Use RavenDB messages persistence by the gateway.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseRavenGatewayPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            config.Configurer.ConfigureComponent<RavenDbPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }

        static Configure SetupGateway(this Configure config)
        {
            GatewayInputAddress = Address.Parse(Configure.EndpointName).SubScope("gateway");

            ConfigureChannels(config);

            ConfigureReceiver(config);

            ConfigureSender(config);

            return config;
        }

        static void ConfigureChannels(Configure config)
        {
            var channelFactory = new ChannelFactory();

            foreach (var type in Configure.TypesToScan.Where(t => typeof(IChannelReceiver).IsAssignableFrom(t) && !t.IsInterface))
                channelFactory.RegisterReceiver(type);

            foreach (var type in Configure.TypesToScan.Where(t => typeof(IChannelSender).IsAssignableFrom(t) && !t.IsInterface))
                channelFactory.RegisterSender(type);

            config.Configurer.RegisterSingleton<IChannelFactory>(channelFactory);
        }

        static void ConfigureSender(Configure config)
        {
            config.Configurer.ConfigureComponent<IdempotentChannelForwarder>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<MainEndpointSettings>(DependencyLifecycle.SingleInstance);

            var configSection = Configure.ConfigurationSource.GetConfiguration<GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
                config.Configurer.ConfigureComponent<ConfigurationBasedChannelManager>(DependencyLifecycle.SingleInstance);
            else
                config.Configurer.ConfigureComponent<ConventionBasedChannelManager>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<GatewaySender>(DependencyLifecycle.SingleInstance);

            ConfigureSiteRouters(config);
        }

        static void ConfigureSiteRouters(Configure config)
        {
            config.Configurer.ConfigureComponent<OriginatingSiteHeaderRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<ConfigurationBasedSiteRouter>(DependencyLifecycle.SingleInstance);
        }

        static void ConfigureReceiver(Configure config)
        {
            config.Configurer.ConfigureComponent<GatewayReceiver>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<IdempotentChannelReceiver>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                                               .ConfigureProperty(x => x.MainInputAddress, Address.Parse(Configure.EndpointName));

        }
    }
}