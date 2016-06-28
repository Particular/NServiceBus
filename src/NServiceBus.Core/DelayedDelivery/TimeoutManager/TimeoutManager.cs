namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using ConsistencyGuarantees;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Faults;
    using Hosting;
    using ObjectBuilder;
    using Persistence;
    using Settings;
    using Support;
    using Timeout.Core;
    using Transports;

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
            Prerequisite(c => !c.DoesTransportSupportConstraint<DelayedDeliveryConstraint>(), "The selected transport supports delayed delivery natively");
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

            var errorQueueAddress = context.Settings.ErrorQueueAddress();
            var requiredTransactionSupport = context.Settings.GetRequiredTransactionModeForReceives();

            SetupStorageSatellite(context, errorQueueAddress, requiredTransactionSupport);

            var dispatcherAddress = SetupDispatcherSatellite(context, errorQueueAddress, requiredTransactionSupport);

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

        static string SetupDispatcherSatellite(FeatureConfigurationContext context, string errorQueueAddress, TransportTransactionMode requiredTransactionSupport)
        {
            var instanceName = context.Settings.EndpointInstanceName();
            var satelliteLogicalAddress = new LogicalAddress(instanceName, "TimeoutsDispatcher");
            var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);
            var failureStorage = new TimeoutFailureInfoStorage();

            context.AddSatelliteReceiver("Timeout Dispatcher Processor", satelliteAddress, requiredTransactionSupport, PushRuntimeSettings.Default, (builder, pushContext) =>
            {
                var recoverabilityBehavior = CreateTimeoutRecoverabilityBehavior(errorQueueAddress, satelliteAddress, builder, failureStorage, context.Settings.EndpointName());
                var dispatchBehavior = new DispatchTimeoutBehavior(
                    builder.Build<IDispatchMessages>(),
                    builder.Build<IPersistTimeouts>(),
                    requiredTransactionSupport);

                return recoverabilityBehavior.Invoke(pushContext, () => dispatchBehavior.Invoke(pushContext));
            });

            return satelliteAddress;
        }

        static void SetupStorageSatellite(FeatureConfigurationContext context, string errorQueueAddress, TransportTransactionMode requiredTransactionSupport)
        {
            var instanceName = context.Settings.EndpointInstanceName();
            var satelliteLogicalAddress = new LogicalAddress(instanceName, "Timeouts");
            var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);
            var failureStorage = new TimeoutFailureInfoStorage();

            context.AddSatelliteReceiver("Timeout Message Processor", satelliteAddress, requiredTransactionSupport, PushRuntimeSettings.Default, (builder, pushContext) =>
            {
                var recoverabilityBehavior = CreateTimeoutRecoverabilityBehavior(errorQueueAddress, satelliteAddress, builder, failureStorage, context.Settings.EndpointName());
                var storeBehavior = new StoreTimeoutBehavior(builder.Build<ExpiredTimeoutsPoller>(), builder.Build<IDispatchMessages>(), builder.Build<IPersistTimeouts>(),
                    context.Settings.EndpointName().ToString());

                return recoverabilityBehavior.Invoke(pushContext, () => storeBehavior.Invoke(pushContext));
            });

            context.Settings.Get<TimeoutManagerAddressConfiguration>().Set(satelliteAddress);
        }

        static TimeoutRecoverabilityBehavior CreateTimeoutRecoverabilityBehavior(string errorQueueAddress, string processorAddress, IBuilder b, TimeoutFailureInfoStorage failureStorage, string endpointName)
        {
            var hostInfo = b.Build<HostInformation>();
            var staticFaultMetadata = new Dictionary<string, string>
                {
                    {FaultsHeaderKeys.FailedQ, processorAddress},
                    {Headers.ProcessingMachine, RuntimeEnvironment.MachineName },
                    {Headers.ProcessingEndpoint, endpointName},
                    {Headers.HostId, hostInfo.HostId.ToString("N")},
                    {Headers.HostDisplayName, hostInfo.DisplayName}
                };

            var recoveryActionExecutor = new RecoveryActionExecutor(b.Build<IDispatchMessages>(), errorQueueAddress, staticFaultMetadata);
            return new TimeoutRecoverabilityBehavior(errorQueueAddress, processorAddress, b.Build<CriticalError>(), failureStorage, recoveryActionExecutor);
        }

        static bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            return settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null;
        }

        TimeSpan TimeToWaitBeforeTriggeringCriticalError = TimeSpan.FromMinutes(2);
    }
}