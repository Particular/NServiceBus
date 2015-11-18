namespace NServiceBus.Features
{
    using System;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DelayedDelivery.TimeoutManager;
    using DeliveryConstraints;
    using DelayedDelivery;
    using NServiceBus.ConsistencyGuarantees;
    using Settings;
    using Timeout.Core;
    using Transports;

    /// <summary>
    ///     Used to configure the timeout manager that provides message deferral.
    /// </summary>
    public class TimeoutManager : Feature
    {
        internal TimeoutManager()
        {
            Defaults(s => s.SetDefault("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", TimeSpan.FromSeconds(2)));
            EnableByDefault();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use the timeoutmanager since it requires receive capabilities");

            Prerequisite(context =>
            {
                var distributorEnabled = context.Settings.GetOrDefault<bool>("Distributor.Enabled");
                var workerEnabled = context.Settings.GetOrDefault<bool>("Worker.Enabled");

                return distributorEnabled || !workerEnabled;
            }, "This endpoint is a worker and will be using the timeoutmanager running at its masternode instead");

            Prerequisite(context => !HasAlternateTimeoutManagerBeenConfigured(context.Settings), "A user configured timeoutmanager address has been found and this endpoint will send timeouts to that endpoint");
            Prerequisite(c => !c.DoesTransportSupportConstraint<DelayedDeliveryConstraint>(), "The selected transport supports delayed delivery natively");
        }

        /// <summary>
        ///     See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            string processorAddress;

            var requiredTransactionSupport = context.Settings.GetRequiredTransactionSupportForReceives();

            var messageProcessorPipeline = context.AddSatellitePipeline("Timeout Message Processor", "Timeouts", requiredTransactionSupport, PushRuntimeSettings.Default, out processorAddress);
            messageProcessorPipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
            messageProcessorPipeline.Register<FirstLevelRetriesBehavior.Registration>();
            messageProcessorPipeline.Register<StoreTimeoutBehavior.Registration>();
            context.Container.ConfigureComponent(b => new StoreTimeoutBehavior(b.Build<ExpiredTimeoutsPoller>(),
                b.Build<IDispatchMessages>(),
                b.Build<IPersistTimeouts>(),
                context.Settings.EndpointName().ToString()), DependencyLifecycle.SingleInstance);

            context.Settings.Get<TimeoutManagerAddressConfiguration>().Set(processorAddress);

            string dispatcherAddress;
            var dispatcherProcessorPipeline = context.AddSatellitePipeline("Timeout Dispatcher Processor", "TimeoutsDispatcher", requiredTransactionSupport, PushRuntimeSettings.Default, out dispatcherAddress);
            dispatcherProcessorPipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
            dispatcherProcessorPipeline.Register<FirstLevelRetriesBehavior.Registration>();
            dispatcherProcessorPipeline.Register("TimeoutDispatcherProcessor", typeof(DispatchTimeoutBehavior), "Dispatches timeout messages");
            context.Container.ConfigureComponent(b => new DispatchTimeoutBehavior(
                b.Build<IDispatchMessages>(),
                b.Build<IPersistTimeouts>(),
                requiredTransactionSupport),
                DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var waitTime = context.Settings.Get<TimeSpan>("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver");

                var criticalError = b.Build<CriticalError>();

                var circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity",
                    waitTime,
                    ex => criticalError.Raise("Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex));

                return new ExpiredTimeoutsPoller(b.Build<IQueryTimeouts>(), b.Build<IDispatchMessages>(), dispatcherAddress, circuitBreaker);
            }, DependencyLifecycle.SingleInstance);
        }

        bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            return settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null;
        }
    }
}