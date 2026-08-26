#nullable enable

namespace NServiceBus;

using Extensibility;

/// <summary>
/// Provides access to the metric tags captured for the message currently being processed.
/// </summary>
public static class MetricTagsExtensions
{
    /// <param name="context">The context to extend.</param>
    extension(ContextBag context)
    {
        /// <summary>
        /// The <see cref="IncomingPipelineMetricTags" /> collected for the message currently being processed. Add to
        /// this collection to have the tags applied to the metrics emitted for that message.
        /// </summary>
        public IncomingPipelineMetricTags MetricTags => context.GetOrCreate<IncomingPipelineMetricTags>();
    }
}
