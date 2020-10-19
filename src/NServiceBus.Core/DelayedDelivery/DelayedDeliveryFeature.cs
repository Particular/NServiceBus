namespace NServiceBus.Features
{
    using DelayedDelivery;
    using DeliveryConstraints;

    class DelayedDeliveryFeature : Feature
    {
        public DelayedDeliveryFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportHasNativeDelayedDelivery = context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();

            if (!transportHasNativeDelayedDelivery)
            {
                context.Pipeline.Register("ThrowIfCannotDeferMessage", new ThrowIfCannotDeferMessageBehavior(), "Throws an exception if an attempt is made to defer a message without infrastructure support.");
            }

            context.Container.ConfigureComponent(b => new NoOpCanceling(), DependencyLifecycle.SingleInstance);
        }
    }
}