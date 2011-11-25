namespace NServiceBus
{
    using System;
    using System.Linq;
    using Config;
    using Gateway.Channels;
    using Gateway.Config;
    using Gateway.Installation;
    using Gateway.Notifications;
    using Gateway.Persistence;
    using Gateway.Persistence.Raven;
    using Gateway.Receiving;
    using Gateway.Routing.Endpoints;
    using Gateway.Routing.Sites;
    using Gateway.Sending;
    using ObjectBuilder;
    using Raven.Client;
    using Persistence.Raven.Config;


    public static class GatewayConfiguration
    {
        public static Address GatewayInputAddress { get; private set; }

        public static Configure Gateway(this Configure config)
        {
            return Gateway(config, typeof(InMemoryPersistence));
        }

        public static Configure GatewayWithInMemoryPersistence(this Configure config)
        {
            return Gateway(config, typeof(InMemoryPersistence));
        }
        public static Configure GatewayWithRavenPersistence(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            return Gateway(config, typeof(RavenDBPersistence));
        }
        public static Configure Gateway(this Configure config, Type persistence)
        {
            config.Configurer.ConfigureComponent(persistence, DependencyLifecycle.SingleInstance);

            return SetupGateway(config);
        }


        static Configure SetupGateway(this Configure config)
        {
            GatewayInputAddress = Address.Local.SubScope("gateway");

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
            config.Configurer.ConfigureComponent<LegacyEndpointRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<IdempotentChannelReceiver>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                                               .ConfigureProperty(x => x.MainInputAddress, Address.Local);

        }
    }

    public class GatewayBootstrapper : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            //todo . introduce a IWantToRunWhenTheBusIsStarted
            Configure.Instance.Builder.Build<IStartableBus>()
                            .Started += (s, e) =>
                                            {
                                                if (!Configure.Instance.Configurer.HasComponent<GatewaySender>())
                                                    return;

                                                Configure.Instance.Builder.Build<GatewaySender>().Start(GatewayConfiguration.GatewayInputAddress);
                                                Configure.Instance.Builder.Build<GatewayReceiver>().Start(GatewayConfiguration.GatewayInputAddress);
                                            };
        }
    }
}