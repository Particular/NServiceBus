namespace NServiceBus.Features
{
    using DelayedDelivery;

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
            DoNotClearTimeouts(context);
        }

        static void DoNotClearTimeouts(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new NoOpCanceling(), DependencyLifecycle.SingleInstance);
        }
    }
}