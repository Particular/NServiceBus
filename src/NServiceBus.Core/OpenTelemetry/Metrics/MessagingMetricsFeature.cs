namespace NServiceBus;

using Features;

/// <summary>
/// MessagingMetricsFeature captures messaging metrics
/// </summary>
class MessagingMetricsFeature : Feature
{
    public MessagingMetricsFeature() => Prerequisite(c => !c.Receiving.IsSendOnlyEndpoint, "Processing metrics are not supported on send-only endpoints");

    /// <inheritdoc />
    protected internal override void Setup(FeatureConfigurationContext context)
    {
        var discriminator = context.Receiving.InstanceSpecificQueueAddress?.Discriminator;
        var queueNameBase = context.Receiving.QueueNameBase;
        var enableMetricTagsCollectionBehavior = new EnableMetricTagsCollectionBehavior();
        var performanceDiagnosticsBehavior = new ReceiveDiagnosticsBehavior(queueNameBase, discriminator);

        context.Pipeline.Register(
            enableMetricTagsCollectionBehavior,
            "Enables OpenTelemetry Metric Tags collection throughout the pipeline"
        );
        context.Pipeline.Register(
            performanceDiagnosticsBehavior,
            "Provides OpenTelemetry counters for message processing"
        );
    }
}