namespace NServiceBus
{
    using System;
    using NServiceBus.Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static class SLAMonitoringConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        public static void EnableSLAPerformanceCounter(this BusConfiguration config, TimeSpan sla)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNegativeAndZero(sla, "sla");
            config.Settings.Set(SLAMonitoring.EndpointSLAKey, sla);
            EnableSLAPerformanceCounter(config);
        }
        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        public static void EnableSLAPerformanceCounter(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            config.EnableFeature<SLAMonitoring>();
        }
    }
}
