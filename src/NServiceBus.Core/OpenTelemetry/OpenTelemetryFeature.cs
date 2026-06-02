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
            new PopulateRecoverabilityTraceMetadataBehavior(),
            "Populates the recoverability metadata"
        );

        var options = context.Settings.GetOrDefault<InstrumentationOptions>();
        if (options?.MessagePayloadAsTag is MessagePayloadAsTag.IncomingMessage or MessagePayloadAsTag.All)
        {
            context.Pipeline.Register(
                new IncomingMessagePayloadToTagsBehavior(),
                "Promotes incoming message properties to span tags");
        }

        if (options?.MessagePayloadAsTag is MessagePayloadAsTag.OutgoingMessage or MessagePayloadAsTag.All)
        {
            context.Pipeline.Register(
                new OutgoingMessagePayloadToTagsBehavior(),
                "Promotes outgoing message properties to span tags");
        }
    }
}