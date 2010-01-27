using System.Configuration;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Distributor
{
    public class Service : IConfigureThisEndpoint, IWantCustomInitialization 
    {
        public static MsmqTransport DataTransport { get; private set; }

        public void Init()
        {
            var configure = Configure.With()
                .Log4Net()
                .SpringBuilder()
                .MsmqTransport()
                    .IsTransactional(true)
                .UnicastBus()
                    .ImpersonateSender(true);

            var numberOfThreads = int.Parse(ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            var errorQueue = ConfigurationManager.AppSettings["ErrorQueue"];

            var nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            var serialization = ConfigurationManager.AppSettings["Serialization"];

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

            DataTransport = new MsmqTransport
            {
                InputQueue = ConfigurationManager.AppSettings["DataInputQueue"],
                NumberOfWorkerThreads = numberOfThreads,
                ErrorQueue = errorQueue,
                IsTransactional = true,
                PurgeOnStartup = false,
                SkipDeserialization = true
            };

            Configure.Instance.Configurer
                .ConfigureProperty<MsmqTransport>(t => t.InputQueue,ConfigurationManager.AppSettings["ControlInputQueue"])
                .ConfigureProperty<MsmqTransport>(t => t.ErrorQueue, errorQueue)
                .ConfigureProperty<MsmqTransport>(t => t.NumberOfWorkerThreads, numberOfThreads);


            Configure.Instance.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(
                ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.StorageQueue, ConfigurationManager.AppSettings["StorageQueue"]);

            Configure.Instance.Configurer.ConfigureComponent<Unicast.Distributor.Distributor>(ComponentCallModelEnum.Singleton);

            configure.LoadMessageHandlers(First<GridInterceptingMessageHandler>
                                              .Then<ReadyMessageHandler>());
        }
    }
}
