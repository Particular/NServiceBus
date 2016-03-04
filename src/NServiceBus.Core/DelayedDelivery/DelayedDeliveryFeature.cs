namespace NServiceBus.Features
{
    using Config;
    using DelayedDelivery;
    using DeliveryConstraints;

    class DelayedDeliveryFeature : Feature
    {
        public DelayedDeliveryFeature()
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
            var transportHasNativeDelayedDelivery = context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();
            var timeoutMgrDisabled = IsTimeoutManagerDisabled(context);

            if (!transportHasNativeDelayedDelivery)
            {
                if (timeoutMgrDisabled)
                {
                    DoNotClearTimeouts(context);
                    context.Pipeline.Register<ThrowIfCannotDeferMessageBehavior.Registration>();
                }
                else
                {
                    var timeoutManagerAddress = context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress;

                    context.Pipeline.Register<RouteDeferredMessageToTimeoutManagerBehavior.Registration>();

                    context.Container.ConfigureComponent(b => new RouteDeferredMessageToTimeoutManagerBehavior(timeoutManagerAddress), DependencyLifecycle.SingleInstance);

                    context.Container.ConfigureComponent(b => new RequestCancelingOfDeferredMessagesFromTimeoutManager(timeoutManagerAddress), DependencyLifecycle.SingleInstance);
                }
            }
            else
            {
                DoNotClearTimeouts(context);
            }
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

        static void DoNotClearTimeouts(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new NoOpCanceling(), DependencyLifecycle.SingleInstance);
        }
    }
}