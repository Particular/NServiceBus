using System;
using System.Collections;
using Common.Logging;
using NServiceBus.Unicast.Transport.Msmq;
using ObjectBuilder;
using NServiceBus.Grid.MessageHandlers;

namespace NServiceBus.Unicast.Distributor.Runner
{
	/// <summary>
	/// Application for creating and executing a <see cref="Distributor"/>.
	/// </summary>
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                MsmqTransport controlTransport = new MsmqTransport();
                controlTransport.InputQueue = System.Configuration.ConfigurationManager.AppSettings["ControlInputQueue"];
                controlTransport.NumberOfWorkerThreads = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
                controlTransport.ErrorQueue = System.Configuration.ConfigurationManager.AppSettings["ErrorQueue"];
                controlTransport.IsTransactional = true;
                controlTransport.PurgeOnStartup = false;
                controlTransport.UseXmlSerialization = false;

                MsmqTransport dataTransport = new MsmqTransport();
                dataTransport.InputQueue = System.Configuration.ConfigurationManager.AppSettings["DataInputQueue"];
                dataTransport.NumberOfWorkerThreads = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NumberOfWorkerThreads"]);
                dataTransport.ErrorQueue = System.Configuration.ConfigurationManager.AppSettings["ErrorQueue"];
                dataTransport.IsTransactional = true;
                dataTransport.PurgeOnStartup = false;
                dataTransport.UseXmlSerialization = false;
                dataTransport.SkipDeserialization = true;

                UnicastBus controlBus = new UnicastBus();
                controlBus.Builder = builder;
                controlBus.Transport = controlTransport;

                ArrayList list = new ArrayList();
                list.Add(typeof(GridInterceptingMessageHandler).Assembly);
                list.Add(typeof(ReadyMessageHandler).Assembly);
                controlBus.MessageHandlerAssemblies = list;

                builder.ConfigureComponent(
                    typeof (MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager),
                    ComponentCallModelEnum.Singleton)
                    .ConfigureProperty("StorageQueue",
                                       System.Configuration.ConfigurationManager.AppSettings["StorageQueue"]);

                Distributor distributor = new Distributor();
                distributor.ControlBus = controlBus;
                distributor.MessageBusTransport = dataTransport;
                distributor.WorkerManager = builder.Build<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>();

                builder.ConfigureComponent(typeof (ReadyMessageHandler), ComponentCallModelEnum.Singlecall)
                    .ConfigureProperty("Bus", controlBus);

                distributor.Start();

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }
    }
}
