namespace NServiceBus.Features
{
    using System;
    using ConsistencyGuarantees;
    using DelayedDelivery;
    using DeliveryConstraints;
    using ObjectBuilder;
    using Persistence;
    using Settings;
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

            var errorQueueAddress = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);
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
            string dispatcherAddress;


            context.AddSatelliteReceiver("Timeout Dispatcher Processor", requiredTransactionSupport, PushRuntimeSettings.Default, "TimeoutsDispatcher",, out dispatcherAddress);

            dispatcherProcessorPipeline.Register("DispatchTimeoutRecoverability",
                b => CreateTimeoutRecoverabilityBehavior(errorQueueAddress, dispatcherAddress, b),
                "Handles failures when dispatching timeouts");

            dispatcherProcessorPipeline.Register("TimeoutDispatcherProcessor", b => new DispatchTimeoutBehavior(
                b.Build<IDispatchMessages>(),
                b.Build<IPersistTimeouts>(),
                requiredTransactionSupport),
                "Terminates the satellite responsible for dispatching expired timeouts to their final destination");
            return dispatcherAddress;
        }

        static void SetupStorageSatellite(FeatureConfigurationContext context, string errorQueueAddress, TransportTransactionMode requiredTransactionSupport)
        {
            //string processorAddress;
            //var messageProcessorPipeline = context.AddSatelliteReceiver("Timeout Message Processor", requiredTransactionSupport, PushRuntimeSettings.Default, "Timeouts", out processorAddress);

            //messageProcessorPipeline.Register("StoreTimeoutRecoverability",
            //    b => CreateTimeoutRecoverabilityBehavior(errorQueueAddress, processorAddress, b),
            //    "Handles failures when storing timeouts");

            //messageProcessorPipeline.Register("StoreTimeoutTerminator", b => new StoreTimeoutBehavior(b.Build<ExpiredTimeoutsPoller>(),
            //    b.Build<IDispatchMessages>(),
            //    b.Build<IPersistTimeouts>(),
            //    context.Settings.EndpointName().ToString()),
            //    "Terminates the satellite responsible for storing timeouts into timeout storage");

            //context.Settings.Get<TimeoutManagerAddressConfiguration>().Set(processorAddress);
        }

        static TimeoutRecoverabilityBehavior CreateTimeoutRecoverabilityBehavior(string errorQueueAddress, string processorAddress, IBuilder b)
        {
            return new TimeoutRecoverabilityBehavior(errorQueueAddress, processorAddress, b.Build<IDispatchMessages>(), b.Build<CriticalError>());
        }

        bool HasAlternateTimeoutManagerBeenConfigured(ReadOnlySettings settings)
        {
            return settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null;
        }

        TimeSpan TimeToWaitBeforeTriggeringCriticalError = TimeSpan.FromMinutes(2);
    }
}