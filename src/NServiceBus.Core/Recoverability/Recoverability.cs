namespace NServiceBus
{
    using System;
    using Config;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Features;

    class Recoverability : Feature
    {
        public Recoverability()
        {
            EnableByDefault();
            DependsOnOptionally<TimeoutManager>();
            Defaults(s =>
            {
                var timeoutManagerAddressConfiguration = new TimeoutManagerAddressConfiguration(s.GetConfigSection<UnicastBusConfig>()?.TimeoutManagerAddress);
                s.Set<TimeoutManagerAddressConfiguration>(timeoutManagerAddressConfiguration);
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var inputQueueAddress = context.Settings.LocalAddress();

            var transportHasNativeDelayedDelivery = context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();
            var timeoutManagerEnabled = !IsTimeoutManagerDisabled(context);
            var timeoutManagerAddress = timeoutManagerEnabled
                ? context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress
                : string.Empty;

            var errorQueueAddress = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            var recoverabilityPolicy = new DefaultRecoverabilityPolicy(new DefaultSecondLevelRetryPolicy(2, TimeSpan.FromSeconds(10)), 2);
            var recoveryExecutor = new RecoveryActionExecutor(recoverabilityPolicy, transportHasNativeDelayedDelivery,
                timeoutManagerEnabled, inputQueueAddress, timeoutManagerAddress, errorQueueAddress);

            context.Container.ConfigureComponent(b => recoveryExecutor, DependencyLifecycle.SingleInstance);
        }

        static bool IsTimeoutManagerDisabled(FeatureConfigurationContext context)
        {
            FeatureState timeoutMgrState;
            if (context.Settings.TryGet("NServiceBus.Features.TimeoutManager", out timeoutMgrState))
            {
                return timeoutMgrState == FeatureState.Deactivated || timeoutMgrState == FeatureState.Disabled;
            }
            return true;
        }
    }
}