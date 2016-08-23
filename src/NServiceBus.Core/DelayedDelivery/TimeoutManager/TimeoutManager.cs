namespace NServiceBus.Features
{
    using System;
    using ConsistencyGuarantees;
    using DelayedDelivery;
    using DelayedDelivery.TimeoutManager;
    using DeliveryConstraints;
    using Persistence;
    using Settings;
    using Timeout.Core;
    using Transport;

    /// <summary>
    /// Used to configure the timeout manager that provides message deferral.
    /// </summary>
    public class TimeoutManager : Feature
    {
        internal TimeoutManager()
        {
            Defaults(s => s.SetDefault("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", TimeToWaitBeforeTriggeringCriticalError));
            EnableByDefault();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use the timeoutmanager since it requires receive capabilities");
            Prerequisite(context => !HasAlternateTimeoutManagerBeenConfigured(context.Settings), "A user configured timeoutmanager address has been found and this endpoint will send timeouts to that endpoint");
            Prerequisite(c => !c.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>(), "The selected transport supports delayed delivery natively");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Timeouts>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for timeout storage. Select another persistence or disable the timeout manager feature using endpointConfiguration.DisableFeature<TimeoutManager>()");
            }

            var requiredTransactionMode = context.Settings.GetRequiredTransactionModeForReceives();

            SetupStorageSatellite(context, requiredTransactionMode);

            var dispatcherAddress = SetupDispatcherSatellite(context, requiredTransactionMode);

            SetupTimeoutPoller(context, dispatcherAddress);

            SetupLegacyTimeoutsSatellite(context, requiredTransactionMode);
        }

        static void SetupTimeoutPoller(FeatureConfigurationContext context, string dispatcherAddress)
        {
            context.Container.ConfigureComponent(b =>
            {
                var waitTime = context.Settings.Get<TimeSpan>("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver");

                var criticalError = b.Build<CriticalError>();

                var circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity",
                    waitTime,
                    ex => criticalError.Raise("Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex));

                return new ExpiredTimeoutsPoller(b.Build<IQueryTimeouts>(), b.Build<IDispatchMessages>(), dispatcherAddress, circuitBreaker, () => DateTime.UtcNow);
            }, DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => new TimeoutPollerRunner(b.Build<ExpiredTimeoutsPoller>()));
        }

        static string SetupDispatcherSatellite(FeatureConfigurationContext context, TransportTransactionMode requiredTransactionSupport)
        {
            var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("TimeoutsDispatcher");
            var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

            context.AddSatelliteReceiver("Timeout Dispatcher Processor", satelliteAddress, requiredTransactionSupport, PushRuntimeSettings.Default, RecoverabilityPolicy,
                (builder, pushContext) =>
                {
                    var dispatchBehavior = new DispatchTimeoutBehavior(
                        builder.Build<IDispatchMessages>(),
                        builder.Build<IPersistTimeouts>(),
                        requiredTransactionSupport);

                    return dispatchBehavior.Invoke(pushContext);
                });

            return satelliteAddress;
        }

        static void SetupStorageSatellite(FeatureConfigurationContext context, TransportTransactionMode requiredTransactionSupport)
        {
            var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("Timeouts");
            var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

            context.AddSatelliteReceiver("Timeout Message Processor", satelliteAddress, requiredTransactionSupport, PushRuntimeSettings.Default, RecoverabilityPolicy,
                (builder, pushContext) =>
                {
                    var storeBehavior = new StoreTimeoutBehavior(
                        builder.Build<ExpiredTimeoutsPoller>(),
                        builder.Build<IDispatchMessages>(),
                        builder.Build<IPersistTimeouts>(),
                        context.Settings.EndpointName().ToString());

                    return storeBehavior.Invoke(pushContext);
                });

            context.Settings.Get<TimeoutManagerAddressConfiguration>().Set(satelliteAddress);
        }

        static void SetupLegacyTimeoutsSatellite(FeatureConfigurationContext context, TransportTransactionMode requiredTransactionMode)
        {
            var satelliteAddress = CreateSatelliteInputQueueAddress(context, "Retries");

            context.AddSatelliteReceiver("Legacy Timeouts Processor", satelliteAddress, requiredTransactionMode, PushRuntimeSettings.Default, RecoverabilityPolicy,
                (builder, pushContext) =>
                {
                    var legacyTimeoutsBehavior = new LegacyTimeoutsBehavior();

                    return legacyTimeoutsBehavior.Invoke(pushContext);
                });
        }

        static bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            return settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null;
        }

        static RecoverabilityAction RecoverabilityPolicy(RecoverabilityConfig config, ErrorContext errorContext)
        {
            if (errorContext.ImmediateProcessingFailures <= MaxNumberOfImmediateRetries)
            {
                return RecoverabilityAction.ImmediateRetry();
            }

            return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
        }

        const int MaxNumberOfImmediateRetries = 4;
        TimeSpan TimeToWaitBeforeTriggeringCriticalError = TimeSpan.FromMinutes(2);
    }
}