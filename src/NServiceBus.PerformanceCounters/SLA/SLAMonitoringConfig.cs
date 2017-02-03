namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Features;

    /// <summary>
    /// Provide configuration options for monitoring related settings.
    /// </summary>
    public static class SLAMonitoringConfig
    {
        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="sla">The <see cref="TimeSpan" /> to use oa the SLA. Must be greater than <see cref="TimeSpan.Zero" />.</param>
        public static void EnableSLAPerformanceCounter2(this EndpointConfiguration config, TimeSpan sla)
        {
       //     Guard.AgainstNull(nameof(config), config);
         //   Guard.AgainstNegativeAndZero(nameof(sla), sla);
            config.GetSettings().Set(SLAMonitoring2.EndpointSLAKey, sla);
            EnableSLAPerformanceCounter2(config);
        }

        /// <summary>
        /// Enables the NServiceBus specific performance counters with a specific EndpointSLA.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableSLAPerformanceCounter2(this EndpointConfiguration config)
        {
            //Guard.AgainstNull(nameof(config), config);

            //disable the core one
            config.DisableFeature<SLAMonitoring>();
            config.EnableFeature<SLAMonitoring2>();
        }
    }
}