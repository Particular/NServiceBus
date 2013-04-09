namespace NServiceBus.Features
{
    using Config;
    using Settings;
    using Timeout.Core;
    using Timeout.Hosting.Windows;
    using Transports;

    /// <summary>
    /// This feature provides message deferral based on a external timeoutmanager.
    /// </summary>
    public class TimeoutManager : IConditionalFeature
    {
        public bool EnabledByDefault()
        {
            return true;
        }
        
        public bool ShouldBeEnabled()
        {
            //has the user already specified a custom deferal method
            if (Configure.HasComponent<IDeferMessages>())
                return false;

            //if we have a master node configured we should use that timeout manager instead
            if (Configure.Instance.GetMasterNodeAddress() != Address.Local )
                return false;

            //send only endpoints doesn't need a TM
            if(SettingsHolder.Get<bool>("Endpoint.SendOnly")) 
                return false;

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //if the user has specified another TM we don't need to run our own
            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
                return false;
            
            return true;
        }

        public void Initialize()
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