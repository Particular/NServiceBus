#nullable enable

namespace NServiceBus;

using Pipeline;
using Transport;

/// <summary>
/// Provides access to the metric tags captured for the message currently being processed.
/// </summary>
public static class MetricTagsExtensions
{
    /// <param name="context">The context to extend.</param>
    extension(IBehaviorContext context)
    {
        /// <summary>
        /// The <see cref="IMetricsTags" /> collected for the message currently being processed. Add to this
        /// collection to have the tags applied to the metrics emitted for that message.
        /// </summary>
        public IMetricsTags MetricTags => context.Extensions.GetOrCreate<PipelineMetricTags>();

        internal PipelineMetricTags PipelineMetricTags => context.Extensions.GetOrCreate<PipelineMetricTags>();
    }

    extension(MessageContext context)
    {
        internal PipelineMetricTags MetricTags => context.Extensions.GetOrCreate<PipelineMetricTags>();
    }
}
