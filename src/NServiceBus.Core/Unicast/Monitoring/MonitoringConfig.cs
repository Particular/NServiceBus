namespace NServiceBus
{
    using System;
    using NServiceBus.Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static partial class MonitoringConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        public static ConfigurationBuilder EnablePerformanceCounters(this ConfigurationBuilder config, TimeSpan sla)
        {
            config.settings.Set(PerformanceMonitoring.EndpointSLAKey, sla);
            config.EnablePerformanceCounters();
            return config;
        }

        /// <summary>
        /// Enables the NServiceBus specific performance counters.
        /// </summary>
        public static ConfigurationBuilder EnablePerformanceCounters(this ConfigurationBuilder config)
        {
            config.EnableFeature<PerformanceMonitoring>();
            return config;
        }
    }
}
