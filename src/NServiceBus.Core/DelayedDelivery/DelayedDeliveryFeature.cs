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
            var transportHasNativeDelayedDelivery = context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();
            var timeoutManagerAddress = context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress;

            if (!transportHasNativeDelayedDelivery)
            {
                if (timeoutManagerAddress == null)
                {
                    DoNotClearTimeouts(context);
                    context.Pipeline.Register("ThrowIfCannotDeferMessage", new ThrowIfCannotDeferMessageBehavior(), "Throws an exception if an attempt is made to defer a message without infrastructure support.");
                }
                else
                {
                    context.Pipeline.Register("RouteDeferredMessageToTimeoutManager", new RouteDeferredMessageToTimeoutManagerBehavior(timeoutManagerAddress), "Reroutes deferred messages to the timeout manager");
                    context.Container.ConfigureComponent(b => new RequestCancelingOfDeferredMessagesFromTimeoutManager(timeoutManagerAddress), DependencyLifecycle.SingleInstance);
                }
            }
            else
            {
                DoNotClearTimeouts(context);
            }
        }

        static void DoNotClearTimeouts(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new NoOpCanceling(), DependencyLifecycle.SingleInstance);
        }
    }
}