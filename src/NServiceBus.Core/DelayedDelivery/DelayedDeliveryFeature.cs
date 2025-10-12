#nullable enable

namespace NServiceBus.Features;

using Transport;

sealed class DelayedDeliveryFeature : Feature
{
    protected internal override void Setup(FeatureConfigurationContext context)
    {
        var transportHasNativeDelayedDelivery = context.Settings.Get<TransportDefinition>().SupportsDelayedDelivery;

        if (!transportHasNativeDelayedDelivery)
        {
            context.Pipeline.Register("ThrowIfCannotDeferMessage", new ThrowIfCannotDeferMessageBehavior(), "Throws an exception if an attempt is made to defer a message without infrastructure support.");
        }
    }
}