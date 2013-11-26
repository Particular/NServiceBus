namespace NServiceBus.Features
{
    using Config;
    using Timeout.Core;
    using Timeout.Hosting.Windows;
    using Transports;

    /// <summary>
    /// This feature provides message deferral based on a external timeout manager.
    /// </summary>
    public class TimeoutManager : Feature
    {
        public override bool IsEnabledByDefault
        {
            get
            {
                return true;
            }
        }

        public override bool ShouldBeEnabled()
        {
            //has the user already specified a custom deferral method
            if (Configure.HasComponent<IDeferMessages>())
                return false;

            //if we have a master node configured we should use the Master Node timeout manager instead
            if (Configure.Instance.HasMasterNode())
                return false;

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //if the user has specified another TM we don't need to run our own
            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
                return false;
            
            return true;
        }

        public override void Initialize()
        {
            DispatcherAddress = Address.Parse(Configure.EndpointName).SubScope("TimeoutsDispatcher");
            InputAddress = Address.Parse(Configure.EndpointName).SubScope("Timeouts");

            Configure.Component<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);

            InfrastructureServices.Enable<IPersistTimeouts>();
            InfrastructureServices.Enable<IManageTimeouts>();
        }

        public static Address InputAddress { get; private set; }
        public static Address DispatcherAddress { get; private set; }
    }
}