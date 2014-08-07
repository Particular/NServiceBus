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
        public static ConfigurationBuilder EnableSLACounter(this ConfigurationBuilder config, TimeSpan sla)
        {
            config.settings.Set(SLAMonitoring.EndpointSLAKey, sla);
            EnableSLACounter(config);
            return config;
        }
        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        public static ConfigurationBuilder EnableSLACounter(this ConfigurationBuilder config)
        {
            config.EnableFeature<SLAMonitoring>();
            return config;
        }

    }
}
