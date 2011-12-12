namespace NServiceBus.Distributor
{
    using Config;
    using MasterNode;
    using Unicast;
    using log4net;

    public class DistributorInitializer:INeedInitialization
    {
        public void Init()
        {
            if (!ConfigureDistributor.DistributorEnabled)
                return;

            var config = Configure.Instance;

            //todo - rework the masternode to a Func<T> instead
            //if this is the master node this will be our own input queue
            var masterNodeAddress = Address.Parse("masternode@localhost");//MasterNodeManager.GetMasterNode();
            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");


            config.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
              DependencyLifecycle.SingleInstance)
              .ConfigureProperty(r => r.DistributorDataQueue, masterNodeAddress);

            if (!ConfigureDistributor.DistributorShouldRunOnThisEndpoint())
                return;

          
            config.Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.InputAddress, masterNodeAddress.SubScope("worker"));

            config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.StorageQueue, masterNodeAddress.SubScope("distributor.storage"));



            var numberOfWorkerThreads = GetNumberOfWorkerThreads();

            config.Configurer.ConfigureComponent<DistributorReadyMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, numberOfWorkerThreads)
                .ConfigureProperty(r => r.ControlQueue, distributorControlAddress);


            config.Configurer.ConfigureComponent<DistributorBootstrapper>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, numberOfWorkerThreads)
                .ConfigureProperty(r => r.InputQueue, masterNodeAddress);
        }

        static int GetNumberOfWorkerThreads()
        {
            var numberOfWorkerThreads = 1;
            var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();

            if (msmqTransport == null)
            {
                Logger.Warn("No transport configuration found so the distributor will default to one thread, for production scenarious you would wan to adjust this setting");
            }
            else
            {
                numberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads;
            }
            return numberOfWorkerThreads;
        }

        static readonly ILog Logger = LogManager.GetLogger("Distributor."+Configure.EndpointName);
       
    }
}