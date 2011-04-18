namespace NServiceBus.Gateway.Config
{
    using System.Configuration;
    using Dispatchers;
    using Gateway;
    using Channels.Http;
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
            string n = ConfigurationManager.AppSettings["NumberOfWorkerThreads"];
           
            var inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            var outputQueue = ConfigurationManager.AppSettings["OutputQueue"];



            int numberOfWorkerThreads;

            if (!int.TryParse(n, out numberOfWorkerThreads))
                numberOfWorkerThreads = 10;


            if(!config.Configurer.HasComponent<IRouteMessages>())
                config.Configurer.ConfigureComponent<LegacyMessageRouter>(DependencyLifecycle.SingleInstance); //todo - use the appconfig as default instead

  
            config.Configurer.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<GatewayService>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ReturnAddress, inputQueue)
               .ConfigureProperty(p => p.DefaultDestinationAddress, outputQueue);

            config.Configurer.ConfigureComponent<TransactionalChannelDispatcher>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<HttpChannelReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ListenUrl, listenUrl)
                .ConfigureProperty(p => p.NumberOfWorkerThreads, numberOfWorkerThreads);

            config.Configurer.ConfigureComponent<HttpChannelSender>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ListenUrl, listenUrl);


            config.Configurer.ConfigureComponent<TransactionalChannelDispatcher>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.InputQueue, inputQueue);
             
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