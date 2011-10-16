using System;
using log4net;
using NServiceBus.Config;
using NServiceBus.Distributor;
using NServiceBus.Distributor.MsmqWorkerAvailabilityManager;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    public class Configurer:IWantToRunBeforeConfiguration
    {
        public static ILog Logger;
        public static string DistributorControlName { get { return "distributor.control"; } }


        public void Init()
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode)
                return;
           
            var config = Configure.Instance;

            var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();

            config.Configurer.ConfigureComponent<ReadyMessageManager>(DependencyLifecycle.SingleInstance);

            Logger = LogManager.GetLogger(Address.Local.SubScope("distributor").Queue);

            var inputQueue = Address.Local;
            var controlQueue = inputQueue.SubScope(DistributorControlName);
            var applicativeInputQueue = inputQueue.SubScope("worker");

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataQueue, inputQueue);

            SetLocalAddress(applicativeInputQueue);

            config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.StorageQueue, inputQueue.SubScope("distributor.storage"));

            config.Configurer.ConfigureComponent<DistributorReadyMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, msmqTransport.NumberOfWorkerThreads)
                .ConfigureProperty(r => r.ControlQueue, controlQueue);


            config.Configurer.ConfigureComponent<DistributorBootstrapper>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, msmqTransport.NumberOfWorkerThreads)
                .ConfigureProperty(r => r.InputQueue, inputQueue);
        }

        private static void SetLocalAddress(Address applicativeInputQueue)
        {
            try
            {
                Address.InitializeLocalAddress(applicativeInputQueue.Queue);
            }
            catch (Exception)
            {
                //intentionally swallow
            }
        }
    }
}
