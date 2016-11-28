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

            // note that the TimeoutManager will be disabled by it's prerequisite check anyway as
            // WorkerFeature sets up an external timeout manager. This line is kept for readability.
            config.DisableFeature<TimeoutManager>();
            config.EnableFeature<WorkerFeature>();
        }
    }
}