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

        public static Configure Distributor(this Configure config)
        {
            var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();
            var inputQueue = GetInputQueue(msmqTransport);

            Logger = LogManager.GetLogger(inputQueue + ".distributor");

            var storageQueue = inputQueue + ".storage";
            var controlQueue = inputQueue + ".control";
            var applicativeInputQueue = inputQueue + ".worker";

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataQueue, inputQueue);

            NServiceBus.Distributor.MsmqWorkerAvailabilityManager.Installer.DistributorActivated = true;

            Configure.ConfigurationComplete +=
                (o, e) =>
                    {
                        var bus = Configure.Instance.Builder.Build<UnicastBus>();
                        bus.Address = applicativeInputQueue;

                        var mgr = new MsmqWorkerAvailabilityManager { StorageQueue = storageQueue };

                        new ReadyMessageManager 
                        { 
                            WorkerAvailabilityManager = mgr,
                            NumberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads,
                            ControlQueue = controlQueue
                        }.Init();

                        new DistributorBootstrapper
                        {
                            WorkerAvailabilityManager = mgr,
                            NumberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads,
                            InputQueue = inputQueue
                        }.Init();
                    };

            return config;
        }

        private static string GetInputQueue(MsmqTransportConfig msmqTransportConfig)
        {
            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();

            var inputQueue = unicastBusConfig.LocalAddress;
            if (inputQueue == null)
                inputQueue = msmqTransportConfig.InputQueue;
            return inputQueue;
        }
    }
}
