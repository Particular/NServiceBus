using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Transport.Msmq;
using ObjectBuilder;
using NServiceBus.Config;
using System.Threading;
using System;

namespace NServiceBus.Unicast.Distributor.Runner
{
    public static class Initalizer
    {
        /// <summary>
        /// Assumes that an <see cref="IMessageSerializer"/> was already configured in the given builder.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static Distributor Init(Func<Configure, Configure> setupSerialization)
        {
            setupSerialization(NServiceBus.Configure.With()
                .SpringBuilder())
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly,
                        typeof(ReadyMessageHandler).Assembly);

            MsmqTransport dataTransport = new MsmqTransport();
            dataTransport.InputQueue = System.Configuration.ConfigurationManager.AppSettings["DataInputQueue"];
            dataTransport.NumberOfWorkerThreads = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
            dataTransport.ErrorQueue = System.Configuration.ConfigurationManager.AppSettings["ErrorQueue"];
            dataTransport.IsTransactional = true;
            dataTransport.PurgeOnStartup = false;
            dataTransport.SkipDeserialization = true;
            dataTransport.Builder = builder;


            MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager mgr = builder.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(ComponentCallModelEnum.Singleton);
            mgr.StorageQueue = System.Configuration.ConfigurationManager.AppSettings["StorageQueue"];

            builder.ConfigureComponent<Distributor>(ComponentCallModelEnum.Singleton);

            Thread.Sleep(100);

            Distributor d = builder.Build<Distributor>();
            d.MessageBusTransport = dataTransport;

            return d;
        }
    }
}
