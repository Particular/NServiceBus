namespace NServiceBus;

using Features;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// MessagingMetricsFeature captures messaging metrics
/// </summary>
class MessagingMetricsFeature : Feature
{
    public MessagingMetricsFeature() => Prerequisite(c => !c.Receiving.IsSendOnlyEndpoint, "Processing metrics are not supported on send-only endpoints");

    /// <inheritdoc />
    protected internal override void Setup(FeatureConfigurationContext context)
    {
        _ = context.Container.AddSingleton<MessagingMetricsMeters>();

        context.Pipeline.Register<EnableMetricTagsCollectionBehavior>(
            new EnableMetricTagsCollectionBehavior(),
            "Enables OpenTelemetry Metric Tags collection throughout the pipeline"
        );
        context.Pipeline.Register(sp =>
            {
                var messagingMetricsMetersMeter = sp.GetRequiredService<MessagingMetricsMeters>();
                return new ReceiveDiagnosticsBehavior(
                    messagingMetricsMetersMeter,
                    context.Receiving.QueueNameBase,
                    context.Receiving.InstanceSpecificQueueAddress?.Discriminator);
            },
            "Provides OpenTelemetry counters for message processing"
        );
    }
}