using System;
using System.Configuration;
using Common.Logging;
using NServiceBus.ObjectBuilder;
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

            var externalTransport = new MsmqTransport
              {
                  InputQueue = ConfigurationManager.AppSettings["ExternalQueue"],
                  NumberOfWorkerThreads = numberOfThreads,
                  MaxRetries = maxRetries,
                  ErrorQueue = errorQueue,
                  IsTransactional = true,
                  PurgeOnStartup = false
              };

            var internalTransport = new MsmqTransport
            {
                InputQueue = ConfigurationManager.AppSettings["InternalQueue"],
                NumberOfWorkerThreads = numberOfThreads,
                MaxRetries = maxRetries,
                ErrorQueue = errorQueue,
                IsTransactional = true,
                PurgeOnStartup = false
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
            proxy.InternalTransport = internalTransport;

            proxy.Start();

            Logger.Info("Proxy successfully started");
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EndpointConfig));
    }
}
