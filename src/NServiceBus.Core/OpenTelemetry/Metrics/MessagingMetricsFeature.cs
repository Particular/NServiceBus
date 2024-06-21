namespace NServiceBus;

using System.Threading.Tasks;
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
        var enableMetricTagsCollectionBehavior = new EnableMetricTagsCollectionBehavior();
        var performanceDiagnosticsBehavior = new ReceiveDiagnosticsBehavior(
            context.Receiving.QueueNameBase,
            context.Receiving.InstanceSpecificQueueAddress?.Discriminator);

        context.Pipeline.Register(
            enableMetricTagsCollectionBehavior,
            "Enables OpenTelemetry Metric Tags collection throughout the pipeline"
        );
        context.Pipeline.Register(
            performanceDiagnosticsBehavior,
            "Provides OpenTelemetry counters for message processing"
        );
        var criticalTimeMetrics = new CriticalTimeMetrics(queueName, discriminator);
        context.Pipeline.OnReceivePipelineCompleted((pipeline, _) => criticalTimeMetrics.Record(pipeline));
    }
}