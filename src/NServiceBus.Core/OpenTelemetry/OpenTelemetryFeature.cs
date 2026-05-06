#nullable enable

namespace NServiceBus;

using Features;

sealed class OpenTelemetryFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Pipeline.Register(
            new OpenTelemetryPublishBehavior(),
            "Manages the depth of the trace for publishes"
        );

        context.Pipeline.Register(
            new OpenTelemetrySendBehavior(),
            "Manages the depth of the trace for sends"
        );

        context.Pipeline.Register(
            new ForceNewParentWhenNecessaryDuringRecoverabilityBehavior(),
            "Overrides the parent trace when necessary during recoverability to avoid creating a child trace of the delayed or failed message's trace"
        );
    }
}