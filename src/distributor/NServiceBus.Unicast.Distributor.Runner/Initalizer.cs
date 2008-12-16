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
        public static void Init(Func<Configure, Configure> setupSerialization)
        {
            MsmqTransport dataTransport = null;

            setupSerialization(NServiceBus.Configure.With()
                .SpringBuilder(
                (cfg =>
                {
                    dataTransport = new MsmqTransport();
                    dataTransport.InputQueue = System.Configuration.ConfigurationManager.AppSettings["DataInputQueue"];
                    dataTransport.NumberOfWorkerThreads = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
                    dataTransport.ErrorQueue = System.Configuration.ConfigurationManager.AppSettings["ErrorQueue"];
                    dataTransport.IsTransactional = true;
                    dataTransport.PurgeOnStartup = false;
                    dataTransport.SkipDeserialization = true;

                    cfg.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(ComponentCallModelEnum.Singleton)
                        .StorageQueue = System.Configuration.ConfigurationManager.AppSettings["StorageQueue"];

                    cfg.ConfigureComponent<Distributor>(ComponentCallModelEnum.Singleton);
                }
                )))
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly,
                        typeof(ReadyMessageHandler).Assembly)
                .CreateBus()
                .Start(builder =>
                    {
                        dataTransport.Builder = builder;

                        var d = builder.Build<Distributor>();
                        d.MessageBusTransport = dataTransport;

                        d.Start();
                    }
                );
        }
    }
}
