namespace NServiceBus.Features
{
    using Transport;

    class DelayedDeliveryFeature : Feature
    {
        public DelayedDeliveryFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportHasNativeDelayedDelivery = context.Settings.Get<TransportDefinition>().SupportsDelayedDelivery;

            if (!transportHasNativeDelayedDelivery)
            {
                context.Pipeline.Register("ThrowIfCannotDeferMessage", new ThrowIfCannotDeferMessageBehavior(), "Throws an exception if an attempt is made to defer a message without infrastructure support.");
            }
        }
    }
}