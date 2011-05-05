namespace NServiceBus.Gateway.Config
{
    using Channels;
    using Gateway;
    using Gateway.Channels.Http;
    using Notifications;
    using Persistence;
    using ObjectBuilder;
    using Persistence.Sql;
    using Routing.Endpoints;
    using Routing.Sites;

    public static class GatewayConfig
    {
        public static Configure GatewayWithInMemoryPersistence(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryPersistence>(DependencyLifecycle.SingleInstance);

            return SetupGateway(config);
        }

        public static Configure Gateway(this Configure config)
        {
            //todo - use DefaultPersistence == raven
            config.Configurer.ConfigureComponent<SqlPersistence>(DependencyLifecycle.SingleInstance);
            return SetupGateway(config);
        }

        private static Configure SetupGateway(this Configure config)
        {   
            //todo - get the configured name - use service locator to get the interface, make sure to do it on config complete
            var endpointName = "MasterEndpoint"; 
            
          
            config.Configurer.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance); 

            config.Configurer.ConfigureComponent<MasterNodeSettings>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyEndpointRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyChannelManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<TransactionalReceiver>(DependencyLifecycle.SingleInstance);
            
            config.Configurer.ConfigureComponent<InputDispatcher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelReceiver>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<InputDispatcher>(DependencyLifecycle.SingleInstance);
             
            Configure.ConfigurationComplete +=
                (o, a) =>
                {
                    Configure.Instance.Builder.Build<IStartableBus>()
                        .Started += (sender, eventargs) =>
                            {
                                var localAddress = endpointName + ".gateway";//todo
                                Configure.Instance.Builder.Build<TransactionalReceiver>().Start(localAddress);
                                Configure.Instance.Builder.Build<InputDispatcher>().Start(localAddress);
                            };
                };

            return config;
        }


    }
}