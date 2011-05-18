﻿namespace NServiceBus
{
    using System;
    using Config;
    using Gateway.Channels.Http;
    using Gateway.Config;
    using Gateway.Installation;
    using Gateway.Notifications;
    using Gateway.Persistence;
    using Gateway.Persistence.Sql;
    using Gateway.Receiving;
    using Gateway.Routing.Endpoints;
    using Gateway.Routing.Sites;
    using Gateway.Sending;
    using ObjectBuilder;

    public static class GatewayConfig
    {
       
        public static Configure Gateway(this Configure config)
        {
            //todo - use DefaultPersistence == raven
            return Gateway(config,typeof(SqlPersistence));
        }

        public static Configure GatewayWithInMemoryPersistence(this Configure config)
        {
            return Gateway(config, typeof(InMemoryPersistence));
        }

        public static Configure Gateway(this Configure config,Type persistence)
        {
            config.Configurer.ConfigureComponent(persistence,DependencyLifecycle.SingleInstance);

            return SetupGateway(config);
        }


        static Configure SetupGateway(this Configure config)
        {
            var gatewayInputAddress = Address.Local.SubScope("gateway");

            config.Configurer.ConfigureComponent<Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.GatewayInputQueue, gatewayInputAddress);

            ConfigureReceiver(config);
            
            ConfigureSender(config);

            ConfigureStartup(gatewayInputAddress);

            return config;
        }

        static void ConfigureStartup(Address gatewayInputAddress)
        {
            Configure.ConfigurationComplete +=
                (o, a) =>
                    {
                        Configure.Instance.Builder.Build<IStartableBus>()
                            .Started += (sender, eventargs) =>
                                {
                                    Configure.Instance.Builder.Build<GatewayReceiver>().Start(gatewayInputAddress);
                                    Configure.Instance.Builder.Build<GatewaySender>().Start(gatewayInputAddress);
                                };
                    };
        }

        static void ConfigureSender(Configure config)
        {
            config.Configurer.ConfigureComponent<GatewaySender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<IdempotentSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<MainEndpointSettings>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyChannelManager>(DependencyLifecycle.SingleInstance);
  
            config.Configurer.ConfigureComponent<GatewaySender>(DependencyLifecycle.SingleInstance);
        }

        static void ConfigureReceiver(Configure config)
        {
            config.Configurer.ConfigureComponent<GatewayReceiver>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyEndpointRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelReceiver>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<IdempotentReceiver>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                                               .ConfigureProperty(x => x.MainInputAddress, Address.Local);

        }
    }
}