using System;
using System.Configuration;
using Common.Logging;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Proxy
{
    class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            var numberOfThreads = int.Parse(ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            var maxRetries = int.Parse(ConfigurationManager.AppSettings["MaxRetries"]);
            var errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];
            var remoteServer = ConfigurationManager.AppSettings["RemoteServer"];

            var externalQueue = new MsmqMessageQueue();
            externalQueue.Init(ConfigurationManager.AppSettings["ExternalQueue"]);
            var externalTransport = new MsmqTransport
              {
                  NumberOfWorkerThreads = numberOfThreads,
                  MaxRetries = maxRetries,
                  IsTransactional = true,
                  MessageQueue = externalQueue
              };

            var internalQueue = new MsmqMessageQueue();
            internalQueue.Init(ConfigurationManager.AppSettings["InternalQueue"]);
            var internalTransport = new MsmqTransport
            {
                NumberOfWorkerThreads = numberOfThreads,
                MaxRetries = maxRetries,
                IsTransactional = true,
                MessageQueue = internalQueue
            };

            var configure = Configure.With().SpringBuilder();

            configure.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.Queue, "NServiceBus_Proxy_Subscriptions");

            configure.Configurer.ConfigureComponent<MsmqProxyDataStorage>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.StorageQueue, "NServiceBus_Proxy_Storage");

            configure.Configurer.ConfigureComponent<Proxy>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.RemoteServer, remoteServer);
            Logger.Info("Proxy configured for remoteserver: " +  remoteServer);

            var proxy = configure.Builder.Build<Proxy>();
            proxy.ExternalTransport = externalTransport;
            proxy.ExternalQueue = externalQueue;
            proxy.InternalTransport = internalTransport;
            proxy.InternalQueue = internalQueue;

            proxy.ExternalAddress = ConfigurationManager.AppSettings["ExternalQueue"];

            proxy.Start();

            Logger.Info("Proxy successfully started");
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EndpointConfig));
    }
}
