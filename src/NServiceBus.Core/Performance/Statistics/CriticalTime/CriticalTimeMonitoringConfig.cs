namespace NServiceBus
{
    using NServiceBus.Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static class CriticalTimeMonitoringConfig
    {

        /// <summary>
        /// Enables the NServiceBus specific performance counters.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static void EnableCriticalTimePerformanceCounter(this BusConfiguration config)
        {
            Guard.AgainstNull("config", config);
            config.EnableFeature<CriticalTimeMonitoring>();
        }
    }
}
