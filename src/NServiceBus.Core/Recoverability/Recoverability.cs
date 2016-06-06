namespace NServiceBus
{
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
            DependsOnOptionally<FirstLevelRetries>();
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
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maxImmediateRetries = transportConfig?.MaxRetries ?? 5;

            if (IsFLRDisabled(context))
            {
                maxImmediateRetries = 0;
            }

            context.Container.ConfigureComponent(b =>
            {
                var recoverabilityPolicy = new DefaultRecoverabilityPolicy(b.Build<SecondLevelRetryPolicy>(), maxImmediateRetries);

                return new RecoveryActionExecutor(recoverabilityPolicy, transportHasNativeDelayedDelivery,
                    timeoutManagerEnabled, inputQueueAddress, timeoutManagerAddress, errorQueueAddress);

            } , DependencyLifecycle.SingleInstance);
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

        static bool IsFLRDisabled(FeatureConfigurationContext context)
        {
            FeatureState timeoutMgrState;
            if (context.Settings.TryGet("NServiceBus.Features.FirstLevelRetries", out timeoutMgrState))
            {
                return timeoutMgrState == FeatureState.Deactivated || timeoutMgrState == FeatureState.Disabled;
            }
            return true;
        }
    }
}