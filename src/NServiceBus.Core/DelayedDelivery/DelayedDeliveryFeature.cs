namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using System.Threading;
    using Transport;

    class DelayedDeliveryFeature : Feature
    {
        public DelayedDeliveryFeature()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            var transportHasNativeDelayedDelivery = context.Settings.Get<TransportDefinition>().SupportsDelayedDelivery;

            if (!transportHasNativeDelayedDelivery)
            {
                context.Pipeline.Register("ThrowIfCannotDeferMessage", new ThrowIfCannotDeferMessageBehavior(), "Throws an exception if an attempt is made to defer a message without infrastructure support.");
            }

            return Task.CompletedTask;
        }
    }
}