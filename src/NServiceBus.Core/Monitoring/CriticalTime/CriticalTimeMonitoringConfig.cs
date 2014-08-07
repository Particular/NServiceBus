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
        public static ConfigurationBuilder EnableCriticalTime(this ConfigurationBuilder config)
        {
            config.EnableFeature<CriticalTimeMonitoring>();
            return config;
        }
    }
}
