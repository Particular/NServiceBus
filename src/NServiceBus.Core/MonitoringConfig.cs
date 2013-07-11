namespace NServiceBus
{
    using System;

    public static class MonitoringConfig
    {
        /// <summary>
        /// Sets the SLA for this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sla"></param>
        /// <returns></returns>
        public static Configure SetEndpointSLA(this Configure config,TimeSpan sla)
        {
            endpointSLA = sla;

            return config;
        }

        /// <summary>
        /// Gets the current SLA for this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static TimeSpan EndpointSLA(this Configure config)
        {
            return endpointSLA;
        }

        static TimeSpan endpointSLA = TimeSpan.Zero; 
        
        /// <summary>
        /// Enables the NServiceBus specific performance counters
        /// </summary>
        /// <returns></returns>
        public static Configure EnablePerformanceCounters(this Configure config)
        {
            performanceCountersEnabled = true;
            return config;
        }

        /// <summary>
        /// True id performance counters are enabled
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool PerformanceCountersEnabled(this Configure config)
        {
            return performanceCountersEnabled;
        }

        static bool performanceCountersEnabled; 
    }
}