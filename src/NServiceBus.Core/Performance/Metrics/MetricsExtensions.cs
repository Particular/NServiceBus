namespace NServiceBus.Performance.Metrics
{
    using System;
    using System.Collections.Generic;
    using Features;

    /// <summary>
    /// Extension methods to configure OpenTelemetry metrics.
    /// </summary>
    public static class MetricsExtensions
    {
        /// <summary>
        /// Configures NServiceBus to report messaging metrics using OpenTelemetry.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static EndpointConfiguration EnableOpenTelemetryMetrics(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.EnableFeature<MessagingMetricsFeature>();

            return config;
        }
    }

    static class Extensions
    {
        public static void ThrowIfSendOnly(this FeatureConfigurationContext context)
        {
            var isSendOnly = context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            if (isSendOnly)
            {
                throw new Exception("Metrics are not supported on send only endpoints.");
            }
        }

        public static bool TryGetMessageType(this ReceivePipelineCompleted completed, out string processedMessageType)
        {
            return completed.ProcessedMessage.Headers.TryGetMessageType(out processedMessageType);
        }

        internal static bool TryGetMessageType(this IReadOnlyDictionary<string, string> headers, out string processedMessageType)
        {
            if (headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageType))
            {
                processedMessageType = enclosedMessageType;
                return true;
            }
            processedMessageType = null;
            return false;
        }
    }
}