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
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="sla">The <see cref="TimeSpan"/> to use for the SLA. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
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
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static void EnableSLAPerformanceCounter(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            config.EnableFeature<SLAMonitoring>();
        }
    }
}
