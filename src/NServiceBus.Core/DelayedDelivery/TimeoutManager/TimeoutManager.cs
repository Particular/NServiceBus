﻿namespace NServiceBus.Features
{
    using System;
    using Config;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Transports;
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
            Defaults(s => s.SetDefault("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", TimeSpan.FromSeconds(2)));
            EnableByDefault();
           
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),"Send only endpoints can't use the timeoutmanager since it requires receive capabilities");
            
            Prerequisite(context =>
            {
                var distributorEnabled = context.Settings.GetOrDefault<bool>("Distributor.Enabled");
                var workerEnabled = context.Settings.GetOrDefault<bool>("Worker.Enabled");

                return distributorEnabled || !workerEnabled;
            },"This endpoint is a worker and will be using the timeoutmanager running at its masternode instead");

            Prerequisite(context => !HasAlternateTimeoutManagerBeenConfigured(context.Settings),"A user configured timeoutmanager address has been found and this endpoint will send timeouts to that endpoint");
            Prerequisite(c => !c.DoesTransportSupportConstraint<DelayedDeliveryConstraint>(), "The selected transport supports delayed delivery natively");

        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();
            var localAddress = context.Settings.LocalAddress();
            var dispatcherAddress = selectedTransportDefinition.GetSubScope(localAddress,"TimeoutsDispatcher");
            var inputAddress = selectedTransportDefinition.GetSubScope(localAddress, "Timeouts");

            var messageProcessorPipeline = context.AddSatellitePipeline("Timeout Message Processor", inputAddress, new TimeoutMessageProcessorBehavior.Registration());
            messageProcessorPipeline.EnableFeature<StoreFaultsInErrorQueue>();
            messageProcessorPipeline.EnableFeature<FirstLevelRetries>();

            context.Container.ConfigureComponent<TimeoutMessageProcessorBehavior>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.InputAddress, inputAddress)
                .ConfigureProperty(t => t.EndpointName, context.Settings.EndpointName());

            var dispatcherProcessorPipeline = context.AddSatellitePipeline("Timeout Dispatcher Processor", dispatcherAddress, new TimeoutDispatcherProcessorBehavior.Registration());
            dispatcherProcessorPipeline.EnableFeature<StoreFaultsInErrorQueue>();
            dispatcherProcessorPipeline.EnableFeature<FirstLevelRetries>();

            context.Container.ConfigureComponent<TimeoutDispatcherProcessorBehavior>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.InputAddress, dispatcherAddress);

            context.Container.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.TimeToWaitBeforeTriggeringCriticalError, context.Settings.Get<TimeSpan>("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver"))
                .ConfigureProperty(t => t.DispatcherAddress, dispatcherAddress)
                ;
            context.Container.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
        }

        bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            var unicastConfig = settings.GetConfigSection<UnicastBusConfig>();

            return unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress);
        }
    }
}