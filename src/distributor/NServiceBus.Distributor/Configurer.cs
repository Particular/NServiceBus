using System;
using log4net;
using NServiceBus.Config;
using NServiceBus.Distributor;
using NServiceBus.Distributor.MsmqWorkerAvailabilityManager;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Distributor;

namespace NServiceBus
{
    public static class Configurer
    {
        public static ILog Logger;
        public static string DistributorControlName { get { return "distributor.control"; } }

        public static Configure Distributor(this Configure config)
        {
            Configure.BeforeInitialization +=
                (o, e) =>
                    {
                        var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();

                        Logger = LogManager.GetLogger(Address.Local.SubScope("distributor").Queue);

                        var inputQueue = Address.Local.Queue;
                        var storageQueue = Address.Local.SubScope("distributor.storage");
                        var controlQueue = Address.Local.SubScope(DistributorControlName);
                        var applicativeInputQueue = Address.Local.SubScope("worker");

                        config.Configurer.ConfigureComponent<ReturnAddressRewriter>(DependencyLifecycle.SingleInstance)
                            .ConfigureProperty(r => r.DistributorDataQueue, inputQueue);

                        config.Configurer.ConfigureComponent<ReadyMessageManager>(DependencyLifecycle.SingleInstance)
                            .ConfigureProperty(r => r.NumberOfWorkerThreads, msmqTransport.NumberOfWorkerThreads);

                        NServiceBus.Distributor.MsmqWorkerAvailabilityManager.Installer.DistributorActivated = true;

                        try
                        {
                            Address.InitializeLocalAddress(applicativeInputQueue.Queue);
                        }
                        catch (Exception)
                        {
                            //intentionally swallow
                        }
                    
                        var mgr = new MsmqWorkerAvailabilityManager { StorageQueue = storageQueue };

                        new DistributorReadyMessageProcessor 
                        { 
                            WorkerAvailabilityManager = mgr,
                            NumberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads
                        }.Init();

                        var d = new DistributorBootstrapper
                            {
                                WorkerAvailabilityManager = mgr,
                                NumberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads,
                                InputQueue = Address.Parse(inputQueue)
                        };
                        d.Init();

                        Configure.ConfigurationComplete += (obj, ev) => d.Start();
                    };

            return config;
        }
    }
}
