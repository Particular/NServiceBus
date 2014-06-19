namespace NServiceBus.Features
{
    using Config;
    using Settings;
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
            
            //send only endpoints deosn't need a a timeout manager
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"));
            
            //if we have a master node configured we should use the Master Node timeout manager instead
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Distributor.Enabled"));

            //if the user has specified another TM we don't need to run our own
            Prerequisite(context => !HasAlternateTimeoutManagerBeenConfigured(context.Settings));
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.Get<string>("EndpointName");

            var dispatcherAddress = Address.Parse(endpointName).SubScope("TimeoutsDispatcher");
            var inputAddress = Address.Parse(endpointName).SubScope("Timeouts");


            context.Container.ConfigureComponent<TimeoutMessageProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t=>t.Disabled,false)
                .ConfigureProperty(t => t.InputAddress, inputAddress)
                .ConfigureProperty(t=>t.EndpointName,context.Settings.EndpointName());

            context.Container.ConfigureComponent<TimeoutDispatcherProcessor>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.Disabled, false)
                .ConfigureProperty(t=>t.InputAddress,dispatcherAddress);

            context.Container.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t=>t.DispatcherAddress,dispatcherAddress);
            context.Container.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
        }

        bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            var unicastConfig = settings.GetConfigSection<UnicastBusConfig>();

            return unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress);
        }
    }
}