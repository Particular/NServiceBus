namespace NServiceBus
{
    using System;

    public static class MonitoringConfig
    {
        /// <summary>
        /// Sets the SLA for this endpoint
        /// </summary>
        public static Configure SetEndpointSLA(this Configure config,TimeSpan sla)
        {
            endpointSLA = sla;

            return config;
        }

        /// <summary>
        /// Gets the current SLA for this endpoint
        /// </summary>
        public static TimeSpan EndpointSLA(this Configure config)
        {
            return endpointSLA;
        }

        static TimeSpan endpointSLA = TimeSpan.Zero; 
        
        /// <summary>
        /// Enables the NServiceBus specific performance counters
        /// </summary>
        public static Configure EnablePerformanceCounters(this Configure config)
        {
            performanceCountersEnabled = true;
            return config;
        }

        /// <summary>
        /// True id performance counters are enabled
        /// </summary>
        public static bool PerformanceCountersEnabled(this Configure config)
        {
            return performanceCountersEnabled;
        }

        static bool performanceCountersEnabled; 
    }
}