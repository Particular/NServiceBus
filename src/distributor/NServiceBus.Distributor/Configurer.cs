﻿using System;
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
            
            Logger = LogManager.GetLogger(Address.Local.SubScope("distributor").Queue);

            var inputQueue = Address.Local.Queue;
            var storageQueue = Address.Local.SubScope("distributor.storage");
            var controlQueue = Address.Local.SubScope("distributor.control");
            var applicativeInputQueue = Address.Local.SubScope("worker");

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataQueue, Address.Local);

            NServiceBus.Distributor.MsmqWorkerAvailabilityManager.Installer.DistributorActivated = true;

            try
            {
                Address.InitializeLocalAddress(applicativeInputQueue.Queue);
            }
            catch (Exception)
            {
                //intentionally swallow
            }

            Configure.ConfigurationComplete +=
                (o, e) =>
                    {
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
                                InputQueue = Address.Parse(inputQueue)
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
