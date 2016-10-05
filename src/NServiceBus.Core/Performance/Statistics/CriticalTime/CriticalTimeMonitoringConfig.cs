namespace NServiceBus
{
    using Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static class CriticalTimeMonitoringConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableCriticalTimePerformanceCounter(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.EnableFeature<CriticalTimeMonitoring>();
        }
    }
}