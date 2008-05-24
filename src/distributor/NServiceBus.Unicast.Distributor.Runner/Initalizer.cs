using System.Collections;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Transport.Msmq;
using ObjectBuilder;

namespace NServiceBus.Unicast.Distributor.Runner
{
    public static class Initalizer
    {
        /// <summary>
        /// Assumes that an <see cref="IMessageSerializer"/> was already configured in the given builder.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static Distributor Init(IBuilder builder)
        {
            MsmqTransport controlTransport = new MsmqTransport();
            controlTransport.InputQueue = System.Configuration.ConfigurationManager.AppSettings["ControlInputQueue"];
            controlTransport.NumberOfWorkerThreads = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            controlTransport.ErrorQueue = System.Configuration.ConfigurationManager.AppSettings["ErrorQueue"];
            controlTransport.IsTransactional = true;
            controlTransport.PurgeOnStartup = false;
            controlTransport.MessageSerializer = builder.Build<IMessageSerializer>();

            MsmqTransport dataTransport = new MsmqTransport();
            dataTransport.InputQueue = System.Configuration.ConfigurationManager.AppSettings["DataInputQueue"];
            dataTransport.NumberOfWorkerThreads = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            dataTransport.ErrorQueue = System.Configuration.ConfigurationManager.AppSettings["ErrorQueue"];
            dataTransport.IsTransactional = true;
            dataTransport.PurgeOnStartup = false;
            dataTransport.SkipDeserialization = true;

            UnicastBus controlBus = new UnicastBus();
            controlBus.Builder = builder;
            controlBus.Transport = controlTransport;

            ArrayList list = new ArrayList();
            list.Add(typeof(GridInterceptingMessageHandler).Assembly);
            list.Add(typeof(ReadyMessageHandler).Assembly);
            controlBus.MessageHandlerAssemblies = list;

            builder.ConfigureComponent(
                typeof(MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager),
                ComponentCallModelEnum.Singleton)
                .ConfigureProperty("StorageQueue",
                                   System.Configuration.ConfigurationManager.AppSettings["StorageQueue"]);

            Distributor distributor = new Distributor();
            distributor.ControlBus = controlBus;
            distributor.MessageBusTransport = dataTransport;
            distributor.WorkerManager = builder.Build<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>();

            builder.ConfigureComponent(typeof(ReadyMessageHandler), ComponentCallModelEnum.Singlecall)
                .ConfigureProperty("Bus", controlBus);

            return distributor;

        }
    }
}
