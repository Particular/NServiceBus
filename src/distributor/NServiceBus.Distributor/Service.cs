using System.Configuration;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport.Msmq;
using ConfigurationException=Common.Logging.ConfigurationException;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast;

namespace NServiceBus.Distributor
{
    public class Service : IConfigureThisEndpoint, IWantCustomInitialization 
    {
        public static MsmqTransport DataTransport { get; private set; }
        public static IMessageQueue MessageSender { get; private set; }

        public void Init()
        {
            var configure = Configure.With()
                .SpringBuilder()
                .XmlSerializer()
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
                    throw new ConfigurationException("Serialization can only be either 'xml', or 'binary'.");
            }

            MessageSender = new MsmqMessageQueue();
            MessageSender.Init(ConfigurationManager.AppSettings["DataInputQueue"]);

            DataTransport = new MsmqTransport
            {
                NumberOfWorkerThreads = numberOfThreads,
                IsTransactional = true,
                MessageQueue = MessageSender
            };



            Configure.Instance.Configurer
                .ConfigureProperty<MsmqTransport>(t => t.NumberOfWorkerThreads, numberOfThreads);
            Configure.Instance.Configurer
                .ConfigureProperty<UnicastBus>(t => t.Address, ConfigurationManager.AppSettings["ControlInputQueue"]);


            Configure.Instance.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(
                ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.StorageQueue, ConfigurationManager.AppSettings["StorageQueue"]);

            Configure.Instance.Configurer.ConfigureComponent<Unicast.Distributor.Distributor>(ComponentCallModelEnum.Singleton);

            configure.LoadMessageHandlers(First<GridInterceptingMessageHandler>
                                              .Then<ReadyMessageHandler>());
        }
    }
}
