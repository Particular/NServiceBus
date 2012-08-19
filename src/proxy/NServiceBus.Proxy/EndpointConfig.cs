using System;
using System.Configuration;
using NServiceBus.Logging;
using NServiceBus.Config;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Proxy
{
    class EndpointConfig : IWantToRunWhenBusStartsAndStops // IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {

        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EndpointConfig));

        /// <summary>
        /// Method called at startup.
        /// </summary>
        public void Start()
        {
            var numberOfThreads = int.Parse(ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            var maxRetries = int.Parse(ConfigurationManager.AppSettings["MaxRetries"]);
            var remoteServer = ConfigurationManager.AppSettings["RemoteServer"];

            var externalTransport = new TransactionalTransport
            {
                NumberOfWorkerThreads = numberOfThreads,
                MaxRetries = maxRetries,
                IsTransactional = !Endpoint.IsVolatile,
                MessageReceiver = new MsmqMessageReceiver()
            };

            var internalTransport = new TransactionalTransport
            {
                NumberOfWorkerThreads = numberOfThreads,
                MaxRetries = maxRetries,
                IsTransactional = !Endpoint.IsVolatile,
                MessageReceiver = new MsmqMessageReceiver()
            };

            //var configure = Configure.With().DefaultBuilder();

            Configure.Instance.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Queue, "NServiceBus_Proxy_Subscriptions");

            Configure.Instance.Configurer.ConfigureComponent<MsmqProxyDataStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.StorageQueue, "NServiceBus_Proxy_Storage");

            Configure.Instance.Configurer.ConfigureComponent<Proxy>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.RemoteServer, Address.Parse(remoteServer));
            Logger.Info("Proxy configured for remoteserver: " + remoteServer);

            var proxy = Configure.Instance.Builder.Build<Proxy>();
            proxy.ExternalTransport = externalTransport;
            proxy.ExternalMessageSender = new MsmqMessageSender();
            proxy.InternalTransport = internalTransport;
            proxy.InternalMessageSender = new MsmqMessageSender();

            var internalQueue = ConfigurationManager.AppSettings["InternalQueue"];
            if (string.IsNullOrEmpty(internalQueue))
                throw new Exception("Required configuration entry 'InternalQueue' is missing.");

            proxy.InternalAddress = Address.Parse(internalQueue);

            var externalQueue = ConfigurationManager.AppSettings["ExternalQueue"];
            if (string.IsNullOrEmpty(externalQueue))
                throw new Exception("Required configuration entry 'ExternalQueue' is missing.");

            proxy.ExternalAddress = Address.Parse(externalQueue);

            proxy.Start();

            Logger.Info("Proxy successfully started");
        }

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
