namespace NServiceBus.Features
{
    using DelayedDelivery;
    using Transport;

    class DelayedDeliveryFeature : Feature
    {
        public DelayedDeliveryFeature()
        {
            EnableByDefault();
            DependsOnOptionally<TimeoutManager>();
            Defaults(s =>
            {
                var timeoutManagerAddressConfiguration = new TimeoutManagerAddressConfiguration(s.GetExternalTimeoutManagerAddress());
                s.Set(timeoutManagerAddressConfiguration);
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportHasNativeDelayedDelivery = context.Settings.Get<TransportDefinition>().SupportsDelayedDelivery;
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
