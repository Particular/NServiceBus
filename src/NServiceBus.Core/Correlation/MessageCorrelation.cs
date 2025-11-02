#nullable enable

namespace NServiceBus.Features;

sealed class MessageCorrelation : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
        => context.Pipeline.Register("AttachCorrelationId", new AttachCorrelationIdBehavior(), "Makes sure that outgoing messages have a correlation id header set");
}