namespace NServiceBus.Distributor.Config
{
    using Logging;
    using Settings;
    using Transports.Msmq.WorkerAvailabilityManager;
    using Unicast;

    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class DistributorInitializer
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
                config.Configurer.ConfigureComponent<MsmqWorkerAvailabilityManager>(
                    DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(r => r.StorageQueueAddress, Address.Local.SubScope("distributor.storage"));
            }

            SettingsHolder.Set("Distributor.Enabled", true);
            SettingsHolder.Set("Distributor.Version", 1);

            Logger.InfoFormat("Endpoint configured to host the distributor, applicative input queue re routed to {0}",
                              applicativeInputQueue);
        }

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Distributor." + Configure.EndpointName);
    }
}