using System;
using log4net;
using NServiceBus.Config;
using NServiceBus.Distributor;
using NServiceBus.Distributor.MsmqWorkerAvailabilityManager;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    public static class Configurer
    {
        public static ILog Logger;
        public static string DistributorControlName { get { return "distributor.control"; } }

        public static Configure Distributor(this Configure config)
        {
            Configure.BeforeInitialization +=
                () =>
                    {
                        var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();

                        config.Configurer.ConfigureComponent<ReadyMessageManager>(DependencyLifecycle.SingleInstance)
                            .ConfigureProperty(r => r.NumberOfWorkerThreads, msmqTransport.NumberOfWorkerThreads);

                        if (RoutingConfig.IsConfiguredAsMasterNode)
                        {
                            Logger = LogManager.GetLogger(Address.Local.SubScope("distributor").Queue);

                            var inputQueue = Address.Local;
                            var storageQueue = inputQueue.SubScope("distributor.storage");
                            //var controlQueue =inputQueue.SubScope(DistributorControlName);
                            var applicativeInputQueue = inputQueue.SubScope("worker");

                            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                                DependencyLifecycle.SingleInstance)
                                .ConfigureProperty(r => r.DistributorDataQueue, inputQueue);

                            Installer.DistributorActivated = true;

                            try
                            {
                                Address.InitializeLocalAddress(applicativeInputQueue.Queue);
                            }
                            catch (Exception)
                            {
                                //intentionally swallow
                            }

                            var mgr = new MsmqWorkerAvailabilityManager {StorageQueue = storageQueue};

                            config.Configurer.RegisterSingleton<MsmqWorkerAvailabilityManager>(mgr);

                            //todo make this one a configured instance instead
                            new DistributorReadyMessageProcessor
                                {
                                    WorkerAvailabilityManager = mgr,
                                    NumberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads
                                }.Init();

                            config.Configurer.ConfigureComponent<DistributorBootstrapper>(DependencyLifecycle.SingleInstance)
                                .ConfigureProperty(r => r.NumberOfWorkerThreads, msmqTransport.NumberOfWorkerThreads)
                                .ConfigureProperty(r => r.InputQueue, Address.Parse(inputQueue));
                        }
                    };

            Configure.ConfigurationComplete +=
                () =>
                    {
                        var bus = config.Builder.Build<IStartableBus>();
                        bus.Started += (obj, ev) => config.Builder.Build<ReadyMessageManager>().Run();
                    };

            return config;
        }
    }
}
