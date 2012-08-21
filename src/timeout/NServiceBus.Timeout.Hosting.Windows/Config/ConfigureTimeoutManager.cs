namespace NServiceBus
{
    using Timeout.Core;
    using Timeout.Core.Dispatch;
    using Unicast.Queuing;
    using Timeout.Hosting.Windows;
    
    public class TimeoutManagerConfiguration : IWantToRunBeforeConfigurationIsFinalized
    {
        private static Address timeoutManagerAddress;
        public ICreateQueues QueueCreator { get; set; }
        
        public void Run()
        {
            Configure.Instance.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<TimeoutRunner>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<TimeoutDispatchHandler>(DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<TimeoutTransportMessageHandler>(DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<TimeoutDispatcher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(d => d.TimeoutManagerAddress, TimeoutManagerAddress);
            Configure.Instance.Configurer.ConfigureComponent<TimeoutMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(mp => mp.InputAddress, TimeoutManagerAddress);
            
            if (!Configure.Instance.Configurer.HasComponent<IPersistTimeouts>())
                ConfigureTimeoutExtensions.DefaultPersistence();

        }
        static Address TimeoutManagerAddress
        {
            get
            {
                return timeoutManagerAddress ?? (timeoutManagerAddress = Address.Parse(Configure.EndpointName).SubScope("Timeouts"));
            }
        }
    }
}