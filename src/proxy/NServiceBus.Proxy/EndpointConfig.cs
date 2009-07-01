using System.Configuration;
using NServiceBus.Host;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Proxy
{
    class EndpointConfig : IConfigureThisEndpoint,
        IWantCustomInitialization,
        IDontWantTheBusStartedAutomatically
    {
        public void Init(Configure configure)
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
                  PurgeOnStartup = false,
                  SkipDeserialization = true
              };

            var internalTransport = new MsmqTransport
            {
                InputQueue = ConfigurationManager.AppSettings["InternalQueue"],
                NumberOfWorkerThreads = numberOfThreads,
                MaxRetries = maxRetries,
                ErrorQueue = errorQueue,
                IsTransactional = true,
                PurgeOnStartup = false,
                SkipDeserialization = true
            };

            configure.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.Queue, "NServiceBus_Proxy_Subscriptions");

            configure.Configurer.ConfigureComponent<InMemoryProxyDataStorage>(ComponentCallModelEnum.Singleton);

            configure.Configurer.ConfigureComponent<Proxy>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.RemoteServer, remoteServer);

            var proxy = configure.Builder.Build<Proxy>();
            proxy.ExternalTransport = externalTransport;
            proxy.InternalTransport = internalTransport;

            proxy.Start();
        }
    }
}
