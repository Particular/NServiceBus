namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provide configuration options for monitoring related settings
    /// </summary>
    public static class MonitoringConfig
    {
        /// <summary>
        /// Sets the SLA for this endpoint
        /// </summary>
        public static Configure SetEndpointSLA(this Configure config,TimeSpan sla)
        {
            EndpointSLA = sla;

            return config;
        }


        internal static TimeSpan EndpointSLA = TimeSpan.Zero; 
        
        /// <summary>
        /// Enables the NServiceBus specific performance counters
        /// </summary>
        public static Configure EnablePerformanceCounters(this Configure config)
        {
            PerformanceCountersEnabled = true;
            return config;
        }


        internal static bool PerformanceCountersEnabled;
    }
}