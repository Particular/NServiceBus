namespace NServiceBus.Features
{
    using Config;
    using Timeout.Core;
    using Timeout.Hosting.Windows;

    /// <summary>
    /// Used to configure the timeout manager that provides message deferral.
    /// </summary>
    public class TimeoutManager : Feature
    {
        internal TimeoutManager()
        {
            DependsOn<TimeoutManagerBasedDeferral>();
            Prerequisite(ShouldRun);
        }

        bool ShouldRun(FeatureConfigurationContext context)
        {
            //if we have a master node configured we should use the Master Node timeout manager instead
            if (context.Settings.GetOrDefault<bool>("Distributor.Enabled"))
            {
                return false;
            }

            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            //if the user has specified another TM we don't need to run our own
            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.Get<string>("EndpointName");

            DispatcherAddress = Address.Parse(endpointName).SubScope("TimeoutsDispatcher");
            InputAddress = Address.Parse(endpointName).SubScope("Timeouts");


            context.Container.ConfigureComponent<TimeoutMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t=>t.Disabled,false)
                .ConfigureProperty(t=>t.EndpointName,context.Settings.EndpointName());
            context.Container.ConfigureComponent<TimeoutDispatcherProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.Disabled, false);

            context.Container.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
        }

        public static Address InputAddress { get; private set; }
        public static Address DispatcherAddress { get; private set; }
    }
}