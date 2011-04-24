namespace NServiceBus.Gateway.Config
{
    using System.Configuration;
    using Channels;
    using Dispatchers;
    using Gateway;
    using Gateway.Channels.Http;
    using Notifications;
    using Persistence;
    using ObjectBuilder;
    using Routing;
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
            //todo add a custom config section for this
            string listenUrl = ConfigurationManager.AppSettings["ListenUrl"];

            //todo - get the configured name
            var endpointName = "MasterEndpoint"; 
            
            //todo get from configsection  or perhaps some other clever soultion to avoid config at all
            var outputQueue = ConfigurationManager.AppSettings["OutputQueue"];



          
            if(!config.Configurer.HasComponent<IRouteMessagesToSites>())
                config.Configurer.ConfigureComponent<KeyPrefixConventionMessageRouter>(DependencyLifecycle.SingleInstance); //todo - use the appconfig as default instead

            config.Configurer.ConfigureComponent<MasterNodeSettings>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<LegacyChannelManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<DefaultChannelFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<GatewayService>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.GatewayInputAddress, endpointName + ".gateway") //todo - move this to a method on the IManageMasterNode
               .ConfigureProperty(p => p.DefaultDestinationAddress, outputQueue);

            config.Configurer.ConfigureComponent<TransactionalChannelDispatcher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelReceiver>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<HttpChannelSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ListenUrl, listenUrl);


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