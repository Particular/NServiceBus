namespace NServiceBus
{
    using Config;
    using Distributor;
    using Distributor.MsmqWorkerAvailabilityManager;
    using Unicast;
    using log4net;

    public static class ConfigureDistributor
    {
        public static bool DistributorEnabled { get; private set; }


        public static ILog Logger;
        public static string DistributorControlName { get { return "distributor.control"; } }

        public static bool DistributorShouldRunOnThisEndpoint()
        {
            return DistributorEnabled && RoutingConfig.IsConfiguredAsMasterNode;
        }


        public static Configure UseDistributor(this Configure config)
        {
            DistributorEnabled = true;

            if (!DistributorShouldRunOnThisEndpoint())
                return config;

            var workerInputQueue = Address.Local.SubScope("worker");

            config.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p=>p.ReturnAddress,workerInputQueue);

            Logger = LogManager.GetLogger(Address.Local.SubScope("distributor").Queue);

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                  DependencyLifecycle.SingleInstance)
                  .ConfigureProperty(r => r.DistributorDataQueue, Address.Local);

            config.Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.InputAddress, workerInputQueue);

            config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.StorageQueue, Address.Local.SubScope("distributor.storage"));



            var numberOfWorkerThreads = GetNumberOfWorkerThreads();

            config.Configurer.ConfigureComponent<DistributorReadyMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, numberOfWorkerThreads)
                .ConfigureProperty(r => r.ControlQueue, Address.Local.SubScope(DistributorControlName));


            config.Configurer.ConfigureComponent<DistributorBootstrapper>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, numberOfWorkerThreads)
                .ConfigureProperty(r => r.InputQueue, Address.Local);

            return config;
        }

        static int GetNumberOfWorkerThreads()
        {
            var numberOfWorkerThreads = 1;
            var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();

            if (msmqTransport == null)
            {
                Logger.Warn("No transport configuration found so the distributor will default to one thread");
            }
            else
            {
                numberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads;
            }
            return numberOfWorkerThreads;
        }


    }
}