namespace NServiceBus.Distributor.Config
{
    using NServiceBus.Config;
    using Unicast;
    using log4net;

    public class DistributorInitializer : INeedInitialization
    {
        public void Init()
        {
            if (!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint())
                return;

            var config = Configure.Instance;

            var masterNodeAddress = config.GetMasterNodeAddress();
            var applicativeInputQueue = masterNodeAddress.SubScope("worker");

            config.Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.InputAddress, masterNodeAddress.SubScope("worker"))
                .ConfigureProperty(r => r.DoNotStartTransport, !config.WorkerShouldRunOnDistributorEndpoint());

            config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.StorageQueue, masterNodeAddress.SubScope("distributor.storage"));

            var numberOfWorkerThreads = GetNumberOfWorkerThreads();

            config.Configurer.ConfigureComponent<DistributorReadyMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, numberOfWorkerThreads)
                .ConfigureProperty(r => r.ControlQueue, masterNodeAddress.SubScope("distributor.control"));


            config.Configurer.ConfigureComponent<DistributorBootstrapper>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.NumberOfWorkerThreads, numberOfWorkerThreads)
                .ConfigureProperty(r => r.InputQueue, masterNodeAddress);

            Logger.InfoFormat("Endpoint configured to host the distributor, applicative input queue re routed to {0}",
                              applicativeInputQueue);
        }



        static int GetNumberOfWorkerThreads()
        {
            var numberOfWorkerThreads = 1;
            var msmqTransport = Configure.GetConfigSection<MsmqTransportConfig>();

            if (msmqTransport == null)
            {
                Logger.Warn(
                    "No transport configuration found so the distributor will default to one thread, for production scenarious you would want to adjust this setting");
            }
            else
            {
                numberOfWorkerThreads = msmqTransport.NumberOfWorkerThreads;
            }
            return numberOfWorkerThreads;
        }

        static readonly ILog Logger = LogManager.GetLogger("Distributor." + Configure.EndpointName);

    }
}