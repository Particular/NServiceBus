namespace NServiceBus;

using Extensibility;

static class MetricsExtensions
{
    const string PipelineMetricsKey = "NServiceBus.Diagnostics.Metrics";

    public static void SetPipelineMetrics(this ContextBag pipelineContext, IMetricsBag metricsBag) => pipelineContext.Set(PipelineMetricsKey, metricsBag);
    public static IMetricsBag GetPipelineMetrics(this ContextBag pipelineContext)
    {
        pipelineContext.TryGet(PipelineMetricsKey, out IMetricsBag metricsBag);
        return metricsBag;
    }
}