using NServiceBus.Logging;

namespace NServiceBus.Distributor.Config
{
    using NServiceBus.Config;
    using Unicast;
    using Unicast.Distributor;

    public class DistributorInitializer
    {
        public static void Init(bool withWorker)
        {
            var config = Configure.Instance;

            var masterNodeAddress = config.GetMasterNodeAddress();
            var applicativeInputQueue = masterNodeAddress.SubScope("worker");

            config.Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.InputAddress, masterNodeAddress.SubScope("worker"))
                .ConfigureProperty(r => r.DoNotStartTransport, !withWorker);

            if (!config.Configurer.HasComponent<IWorkerAvailabilityManager>())
            {
                config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager.MsmqWorkerAvailabilityManager>(
                    DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(r => r.StorageQueueAddress, masterNodeAddress.SubScope("distributor.storage"));
            }

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
                    "No transport configuration found so the distributor will default to one thread, for production scenarios you would want to adjust this setting");
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