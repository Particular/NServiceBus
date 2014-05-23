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
        public TimeoutManager()
        {
            EnableByDefault();
            Prerequisite(ShouldRun);
        }

        bool ShouldRun(FeatureConfigurationContext context)
        {
            //has the user already specified a custom deferral method
            if (context.Container.HasComponent<IDeferMessages>())
            {
                return false;
            }

            //if we have a master node configured we should use the Master Node timeout manager instead
            if (context.Settings.GetOrDefault<bool>("Distributor.Enabled"))
            {
                return false;
            }

            var unicastConfig = Configure.Instance.GetConfigSection<UnicastBusConfig>();

            //if the user has specified another TM we don't need to run our own
            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return false;
            }
            
            return true;
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.Get<string>("EndpointName");

            DispatcherAddress = Address.Parse(endpointName).SubScope("TimeoutsDispatcher");
            InputAddress = Address.Parse(endpointName).SubScope("Timeouts");


            context.Container.ConfigureComponent<TimeoutMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t=>t.Disabled,false);
            context.Container.ConfigureComponent<TimeoutDispatcherProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.Disabled, false);

            context.Container.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
        }

        public static Address InputAddress { get; private set; }
        public static Address DispatcherAddress { get; private set; }
    }
}