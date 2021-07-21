namespace NServiceBus.Features
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
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
            Prerequisite(c => !c.Settings.Get<TransportDefinition>().SupportsDelayedDelivery || IsMigrationModeEnabled(c.Settings), "The selected transport supports delayed delivery natively");
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

                var criticalError = b.GetRequiredService<CriticalError>();

                var circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity",
                    waitTime,
                    ex => criticalError.Raise("Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex));

                return new ExpiredTimeoutsPoller(b.GetRequiredService<IQueryTimeouts>(), b.GetRequiredService<IDispatchMessages>(), dispatcherAddress, circuitBreaker, () => DateTimeOffset.UtcNow);
            }, DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => new TimeoutPollerRunner(b.GetRequiredService<ExpiredTimeoutsPoller>()));
        }

        static string SetupDispatcherSatellite(FeatureConfigurationContext context, PushRuntimeSettings pushRuntimeSettings)
        {
            var satelliteLogicalAddress = new QueueAddress(context.Receiving.LocalAddress, null, null, "TimeoutsDispatcher");
            var satelliteAddress = context.Receiving.transportSeam.TransportDefinition.ToTransportAddress(satelliteLogicalAddress); // TODO: Unknown how to get access to current transport definition.
            var requiredTransactionSupport = context.Receiving.transportSeam.TransportDefinition.TransportTransactionMode;

            context.AddSatelliteReceiver("Timeout Dispatcher Processor", satelliteAddress, pushRuntimeSettings, RecoverabilityPolicy,
                (builder, messageContext, cancellationToken) =>
                {
                    var dispatchBehavior = new DispatchTimeoutBehavior(
                        builder.GetRequiredService<IDispatchMessages>(),
                        builder.GetRequiredService<IPersistTimeouts>(),
                        requiredTransactionSupport);

                    return dispatchBehavior.Invoke(messageContext); // TODO: Maybe do something with CT here?
                });

            return satelliteAddress;
        }

        static void SetupStorageSatellite(FeatureConfigurationContext context, PushRuntimeSettings pushRuntimeSettings)
        {
            var satelliteLogicalAddress = new QueueAddress(context.Receiving.LocalAddress, null, null, "Timeouts");
            var satelliteAddress = context.Receiving.transportSeam.TransportDefinition.ToTransportAddress(satelliteLogicalAddress);

            context.AddSatelliteReceiver("Timeout Message Processor", satelliteAddress, pushRuntimeSettings, RecoverabilityPolicy,
                (builder, messageContext, cancellationToken) =>
                {
                    var storeBehavior = new StoreTimeoutBehavior(
                        builder.GetRequiredService<ExpiredTimeoutsPoller>(),
                        builder.GetRequiredService<IDispatchMessages>(),
                        builder.GetRequiredService<IPersistTimeouts>(),
                        context.Settings.EndpointName());

                    return storeBehavior.Invoke(messageContext); // TODO: Maybe do something with CT here?
                });

            context.Settings.Get<TimeoutManagerAddressConfiguration>().Set(satelliteAddress);
        }

        static bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            return settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null;
        }

        static bool IsMigrationModeEnabled(ReadOnlySettings settings)
        {
            // this key can be set by transports once they provide native support for delayed messages.
            return settings.TryGet("NServiceBus.TimeoutManager.EnableMigrationMode", out bool enabled) && enabled;
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
