namespace NServiceBus.Distributor.Config
{
    using Logging;
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

            Logger.InfoFormat("Endpoint configured to host the distributor, applicative input queue re routed to {0}",
                              applicativeInputQueue);
        }

        static readonly ILog Logger = LogManager.GetLogger("Distributor." + Configure.EndpointName);
    }
}