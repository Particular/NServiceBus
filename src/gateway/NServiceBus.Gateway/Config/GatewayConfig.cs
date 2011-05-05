namespace NServiceBus
{
    using System;
    using Gateway;
    using Gateway.Channels;
    using Gateway.Channels.Http;
    using Gateway.Config;
    using Gateway.Notifications;
    using Gateway.Persistence;
    using Gateway.Persistence.Sql;
    using Gateway.Routing.Endpoints;
    using Gateway.Routing.Sites;
    using ObjectBuilder;
    using Unicast;

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
            ConfigureReceiver(config);
            
            ConfigureSender(config);

            ConfigureAddresses();

            return config;
        }

        static void ConfigureAddresses()
        {
            Configure.ConfigurationComplete +=
                (o, a) =>
                    {
                        var mainInputAddress = Configure.Instance.Builder.Build<UnicastBus>().Address;

                        Configure.Instance.Configurer.ConfigureProperty<DefaultEndpointRouter>(x => x.MainInputAddress,
                                                                                               mainInputAddress);

                        Configure.Instance.Builder.Build<IStartableBus>()
                            .Started += (sender, eventargs) =>
                                {
                                    var localAddress = mainInputAddress + ".gateway"; //todo - should have the config enforce local addresses? Check with Udi
                                    Configure.Instance.Builder.Build<GatewayReceiver>().Start(localAddress);
                                    Configure.Instance.Builder.Build<GatewaySender>().Start(localAddress);
                                };
                    };
        }

        static void ConfigureSender(Configure config)
        {
            config.Configurer.ConfigureComponent<GatewaySender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<IdempotentTransmitter>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<MasterNodeSettings>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyChannelManager>(DependencyLifecycle.SingleInstance);
  
            config.Configurer.ConfigureComponent<GatewaySender>(DependencyLifecycle.SingleInstance);
        }

        static void ConfigureReceiver(Configure config)
        {
            config.Configurer.ConfigureComponent<GatewayReceiver>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyEndpointRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<HttpChannelReceiver>(DependencyLifecycle.InstancePerCall);
        }
    }
}