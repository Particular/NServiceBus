namespace NServiceBus.Routing.Legacy
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Features;

    /// <summary>
    /// Extension methods to configure Distributor.
    /// </summary>
    public static class ConfigureMSMQDistributor
    {
        /// <summary>
        /// Enlist Worker with Master node defined in the config.
        /// </summary>
        public static void EnlistWithLegacyMSMQDistributor(this EndpointConfiguration config, string masterNodeAddress, string masterNodeControlAddress, int capacity)
        {
            if (masterNodeAddress == null)
            {
                throw new ArgumentNullException(nameof(masterNodeAddress));
            }
            config.GetSettings().Set("LegacyDistributor.Address", masterNodeAddress);
            config.GetSettings().Set("LegacyDistributor.ControlAddress", masterNodeControlAddress);
            config.GetSettings().Set("LegacyDistributor.Capacity", capacity);
            config.DisableFeature<TimeoutManager>();
            config.EnableFeature<WorkerFeature>();
        }
    }
}