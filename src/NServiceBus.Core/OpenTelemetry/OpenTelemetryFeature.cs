namespace NServiceBus;

using Features;

sealed class OpenTelemetryFeature : Feature, IFeatureFactory
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
    }

    static Feature IFeatureFactory.Create() => new OpenTelemetryFeature();
}