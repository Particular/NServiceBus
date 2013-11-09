namespace NServiceBus.Distributor.MSMQ.Config
{
    using Logging;
    using Unicast;

    internal class DistributorInitializer
    {
        public static void Init(bool withWorker)
        {
            var config = Configure.Instance;

            var applicativeInputQueue = Address.Local.SubScope("worker");

            config.Configurer.ConfigureComponent<UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.InputAddress, applicativeInputQueue)
                .ConfigureProperty(r => r.DoNotStartTransport, !withWorker);

            if (!config.Configurer.HasComponent<IWorkerAvailabilityManager>())
            {
                config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager>(DependencyLifecycle.SingleInstance);
            }

            Logger.InfoFormat("Endpoint configured to host the distributor, applicative input queue re routed to {0}",
                applicativeInputQueue);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(DistributorInitializer));
    }
}