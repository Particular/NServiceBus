namespace NServiceBus.Features
{
    using System;
    using NServiceBus.CircuitBreakers;
    using NServiceBus.Config;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DelayedDelivery.TimeoutManager;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout.Core;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    /// <summary>
    /// Allows to delay sent messages.
    /// </summary>
    public class DelayedDelivery : Feature
    {
        internal DelayedDelivery()
        {
            EnableByDefault();
            //TM
            Defaults(s => s.SetDefault("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", TimeSpan.FromSeconds(2)));

            Prerequisite(c => c.DoesTransportSupportConstraint<DelayedDeliveryConstraint>()
                              || !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly")
                              || c.Settings.GetConfigSection<UnicastBusConfig>()?.TimeoutManagerAddress != null,
                              "Transport does not support delayed delivery natively or endpoint is configured as send only and no external timeout manager is configured.");
        }

        /// <summary>
        ///     Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("ApplyDelayedDeliveryConstraint", typeof(ApplyDelayedDeliveryConstraintBehavior), "Applied relevant delayed delivery constraints requested by the user");

            var transportHasNativeDelayedDelivery = context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();
            if (transportHasNativeDelayedDelivery)
            {
                return;
            }

            var processorAddress = GetExternalTimeoutManagerAddress(context) ?? RegisterLocalTimeoutManager(context);

            context.Pipeline.Register<RouteDeferredMessageToTimeoutManagerBehavior.Registration>();
            context.Container.ConfigureComponent(b => new RouteDeferredMessageToTimeoutManagerBehavior(processorAddress), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                var dispatchPipeline = new PipelineBase<DispatchContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                return new RequestCancelingOfDeferredMessagesFromTimeoutManager(processorAddress, dispatchPipeline);
            }, DependencyLifecycle.SingleInstance);
        }

        static string GetExternalTimeoutManagerAddress(FeatureConfigurationContext context)
        {
            return context.Settings.GetConfigSection<UnicastBusConfig>()?.TimeoutManagerAddress;
        }

        static string RegisterLocalTimeoutManager(FeatureConfigurationContext context)
        {
            var consistencyGuarantee = context.Settings.Get<TransportDefinition>().GetDefaultConsistencyGuarantee();
            string processorAddress;
            var messageProcessorPipeline = context.AddSatellitePipeline("Timeout Message Processor", "Timeouts", consistencyGuarantee, PushRuntimeSettings.Default, out processorAddress);
            messageProcessorPipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
            messageProcessorPipeline.Register<FirstLevelRetriesBehavior.Registration>();
            messageProcessorPipeline.Register<StoreTimeoutBehavior.Registration>();
            context.Container.ConfigureComponent(b => new StoreTimeoutBehavior(b.Build<ExpiredTimeoutsPoller>(),
                b.Build<IDispatchMessages>(),
                b.Build<IPersistTimeouts>(),
                context.Settings.EndpointName().ToString()), DependencyLifecycle.SingleInstance);

            string dispatcherAddress;
            var dispatcherProcessorPipeline = context.AddSatellitePipeline("Timeout Dispatcher Processor", "TimeoutsDispatcher", consistencyGuarantee, PushRuntimeSettings.Default, out dispatcherAddress);
            dispatcherProcessorPipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
            dispatcherProcessorPipeline.Register<FirstLevelRetriesBehavior.Registration>();
            dispatcherProcessorPipeline.Register<DispatchTimeoutBehavior.Registration>();

            context.Container.ConfigureComponent(b =>
            {
                var waitTime = context.Settings.Get<TimeSpan>("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver");

                var criticalError = b.Build<CriticalError>();

                var circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity",
                    waitTime,
                    ex => criticalError.Raise("Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex));

                return new ExpiredTimeoutsPoller(b.Build<IQueryTimeouts>(), b.Build<IDispatchMessages>(), dispatcherAddress, circuitBreaker);
            }, DependencyLifecycle.SingleInstance);
            return processorAddress;
        }
    }
}