namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    [ObsoleteEx(
        Message = "Performance counters have been released as a separate package: NServiceBus.WindowsPerformanceCounters",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public static class SLAMonitoringConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="sla">The <see cref="TimeSpan" /> to use oa the SLA. Must be greater than <see cref="TimeSpan.Zero" />.</param>
        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.WindowsPerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void EnableSLAPerformanceCounter(this EndpointConfiguration config, TimeSpan sla)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNegativeAndZero(nameof(sla), sla);
            config.Settings.Set(SLAMonitoring.EndpointSLAKey, sla);
            EnableSLAPerformanceCounter(config);
        }

        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        [ObsoleteEx(
            Message = "Performance counters have been released as a separate package: NServiceBus.WindowsPerformanceCounters",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public static void EnableSLAPerformanceCounter(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.EnableFeature<SLAMonitoring>();
        }
    }
}