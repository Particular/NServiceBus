namespace NServiceBus.Features
{
    using System;
    using ConsistencyGuarantees;
    using DelayedDelivery;
    using DeliveryConstraints;
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

            var pushRuntimeSettings = context.Settings.GetTimeoutManagerMaxConcurrency();

            SetupStorageSatellite(context, pushRuntimeSettings);

            var dispatcherAddress = SetupDispatcherSatellite(context, pushRuntimeSettings);

            SetupTimeoutPoller(context, dispatcherAddress);
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

        static string SetupDispatcherSatellite(FeatureConfigurationContext context, PushRuntimeSettings pushRuntimeSettings)
        {
            var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("TimeoutsDispatcher");
            var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);
            var requiredTransactionSupport = context.Settings.GetRequiredTransactionModeForReceives();

            context.AddSatelliteReceiver("Timeout Dispatcher Processor", satelliteAddress, pushRuntimeSettings, RecoverabilityPolicy,
                (builder, messageContext) =>
                {
                    var dispatchBehavior = new DispatchTimeoutBehavior(
                        builder.Build<IDispatchMessages>(),
                        builder.Build<IPersistTimeouts>(),
                        requiredTransactionSupport);

                    return dispatchBehavior.Invoke(messageContext);
                });

            return satelliteAddress;
        }

        static void SetupStorageSatellite(FeatureConfigurationContext context, PushRuntimeSettings pushRuntimeSettings)
        {
            var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("Timeouts");
            var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

            context.AddSatelliteReceiver("Timeout Message Processor", satelliteAddress, pushRuntimeSettings, RecoverabilityPolicy,
                (builder, messageContext) =>
                {
                    var storeBehavior = new StoreTimeoutBehavior(
                        builder.Build<ExpiredTimeoutsPoller>(),
                        builder.Build<IDispatchMessages>(),
                        builder.Build<IPersistTimeouts>(),
                        context.Settings.EndpointName().ToString());

                    return storeBehavior.Invoke(messageContext);
                });

            context.Settings.Get<TimeoutManagerAddressConfiguration>().Set(satelliteAddress);
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