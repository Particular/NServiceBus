namespace NServiceBus
{
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
}