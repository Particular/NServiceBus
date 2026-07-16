#nullable enable

namespace NServiceBus;

using Features;

sealed class OpenTelemetryFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        var instrumentationOptions = context.Settings.GetOrDefault<InstrumentationOptions>() ?? new InstrumentationOptions();

        context.Pipeline.Register(
            new OpenTelemetryPublishBehavior(instrumentationOptions),
            "Manages the depth of the trace for publishes"
        );

        context.Pipeline.Register(
            new OpenTelemetrySendBehavior(instrumentationOptions),
            "Manages the depth of the trace for sends"
        );

        context.Pipeline.Register(
            new OpenTelemetryDelayedMessageBehavior(instrumentationOptions),
            "Manages the depth of the trace for delayed messages"
        );

        context.Pipeline.Register(
            new PopulateRecoverabilityTraceMetadataBehavior(instrumentationOptions),
            "Populates the recoverability metadata"
        );
    }
}