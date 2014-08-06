namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static partial class MonitoringConfig
    {

        internal static bool TryGetEndpointSLA(this ReadOnlySettings settings, out TimeSpan endpointSLA)
        {
            return settings.TryGet("EndpointSLA", out endpointSLA);
        }

        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        public static ConfigurationBuilder EnablePerformanceCounters(this ConfigurationBuilder config, TimeSpan sla)
        {
            config.settings.Set("EndpointSLA", sla);
            config.EnablePerformanceCounters();
            return config;
        }

        /// <summary>
        /// Enables the NServiceBus specific performance counters.
        /// </summary>
        public static ConfigurationBuilder EnablePerformanceCounters(this ConfigurationBuilder config)
        {
            config.settings.Set("PerformanceCountersEnabled", true);
            config.EnableFeature<PerformanceMonitoring>();
            return config;
        }

        internal static bool GetPerformanceCountersEnabled(this ReadOnlySettings settings)
        {
            bool performanceCountersEnabled;
            settings.TryGet("PerformanceCountersEnabled", out performanceCountersEnabled);
            return performanceCountersEnabled;
        }

    }
}
