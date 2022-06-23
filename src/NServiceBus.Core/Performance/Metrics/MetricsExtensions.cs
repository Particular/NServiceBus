namespace NServiceBus.Performance.Metrics
{
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods to configure OpenTelemetry metrics.
    /// </summary>
    public static class MetricsExtensions
    {
        /// <summary>
        /// Configures NServiceBus to report messaging metrics using OpenTelemetry.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableOpenTelemetryMetrics(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.EnableFeature<MessagingMetricsFeature>();
        }
    }

    static class Extensions
    {
        public static bool TryGetMessageTypes(this ReceivePipelineCompleted completed, out string processedMessageTypes)
        {
            return completed.ProcessedMessage.Headers.TryGetMessageType(out processedMessageTypes);
        }

        internal static bool TryGetMessageType(this IReadOnlyDictionary<string, string> headers, out string processedMessageTypes)
        {
            return headers.TryGetValue(Headers.EnclosedMessageTypes, out processedMessageTypes);
        }
    }
}