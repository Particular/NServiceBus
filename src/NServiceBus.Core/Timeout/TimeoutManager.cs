﻿namespace NServiceBus.Features
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
           
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),"Send only endpoints can't use the timeoutmanager since it requires receive capabilities");
            
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Distributor.Enabled"),"This endpoint is a worker and will be using the timeoutmanager running at its masternode instead");

            Prerequisite(context => !HasAlternateTimeoutManagerBeenConfigured(context.Settings),"A user configured timeoutmanager address has been found and this endpoint will send timeouts to that endpoint");
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