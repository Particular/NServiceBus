using System.Configuration;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Distributor
{
    public class Service : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            var numberOfThreads = int.Parse(ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            var errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];

            var nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            var serialization = ConfigurationManager.AppSettings["Serialization"];

            var configure = Configure.With()
                .DefaultBuilder()
                .CustomConfigurationSource(new Custom { ErrorQueue = errorQueue })
                .MsmqTransport()
                    .IsTransactional(true)
                .UnicastBus()
                    .ImpersonateSender(true);

            switch (serialization)
            {
                case "xml":
                    configure.XmlSerializer(nameSpace);
                    break;
                case "binary":
                    configure.BinarySerializer();
                    break;
                default:
                    throw new ConfigurationErrorsException("Serialization can only be either 'xml', or 'binary'.");
            }


            ConfigureDistributor(numberOfThreads);

            Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(t => t.NumberOfWorkerThreads, numberOfThreads);

            Configure.Instance.Configurer.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(t => t.Address, ConfigurationManager.AppSettings["ControlInputQueue"]);


            Configure.Instance.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(
                ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.StorageQueue, ConfigurationManager.AppSettings["StorageQueue"]);
           
            configure.LoadMessageHandlers(First<GridInterceptingMessageHandler>
                                              .Then<ReadyMessageHandler>());
        }

        void ConfigureDistributor(int numberOfThreads)
        {
            var dataTransport = new TransactionalTransport
                                    {
                                        NumberOfWorkerThreads = numberOfThreads,
                                        IsTransactional = true,
                                        MessageReceiver = new MsmqMessageReceiver()
                                    };

            var distributor = new Unicast.Distributor.Distributor
                                  {
                                      MessageBusTransport = dataTransport,
                                      MessageSender = new MsmqMessageSender(),
                                      DataTransportInputQueue = ConfigurationManager.AppSettings["DataInputQueue"]
                                  };

            Configure.Instance.Configurer.RegisterSingleton<Unicast.Distributor.Distributor>(distributor);
        }
    }

    public class Custom : IConfigurationSource
    {
        public string ErrorQueue { get; set; }
        public string InputQueue { get; set; }

        public T GetConfiguration<T>() where T : class, new()
        {
            if (typeof(T) == typeof(MsmqTransportConfig))
                return new MsmqTransportConfig { ErrorQueue = ErrorQueue, InputQueue = InputQueue } as T;

            return null;
        }
    }
}
