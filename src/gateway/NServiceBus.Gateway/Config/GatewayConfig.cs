namespace NServiceBus.Gateway.Config
{
    using Channels;
    using Dispatchers;
    using Gateway;
    using Gateway.Channels.Http;
    using Notifications;
    using Persistence;
    using ObjectBuilder;
    using Routing;
    using Routing.Endpoints;
    using Routing.Routers;

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
            //todo - get the configured name
            var endpointName = "MasterEndpoint"; 
            
          
            if(!config.Configurer.HasComponent<IRouteMessagesToSites>())
                config.Configurer.ConfigureComponent<KeyPrefixConventionMessageRouter>(DependencyLifecycle.SingleInstance); //todo - use the appconfig as default instead

            config.Configurer.ConfigureComponent<MasterNodeSettings>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyEndpointRouter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyChannelManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DefaultChannelFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<GatewayService>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.GatewayInputAddress, endpointName + ".gateway"); //todo - move this to a method on the IManageMasterNode
            
            config.Configurer.ConfigureComponent<TransactionalChannelDispatcher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelReceiver>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<TransactionalChannelDispatcher>(DependencyLifecycle.SingleInstance);
             
            Configure.ConfigurationComplete +=
                (o, a) =>
                {
                    Configure.Instance.Builder.Build<IStartableBus>()
                        .Started += (sender, eventargs) => Configure.Instance.Builder.Build<GatewayService>().Start();
                };

            return config;
        }


    }
}