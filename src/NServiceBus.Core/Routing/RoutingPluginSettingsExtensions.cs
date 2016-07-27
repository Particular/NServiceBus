namespace NServiceBus
{
    using Features;
    using Routing;

    /// <summary>
    /// Provides convinient API for plugins that need to extend the routing.
    /// </summary>
    public static class RoutingPluginSettingsExtensions
    {
        /// <summary>
        /// Returns the routing table.
        /// </summary>
        /// <param name="featureConfigurationContext">Context.</param>
        public static UnicastRoutingTable RoutingTable(this FeatureConfigurationContext featureConfigurationContext)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            return featureConfigurationContext.Settings.Get<UnicastRoutingTable>();
        }

        /// <summary>
        /// Returns the routing table.
        /// </summary>
        /// <param name="featureConfigurationContext">Context.</param>
        public static EndpointInstances EndpointInstances(this FeatureConfigurationContext featureConfigurationContext)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            return featureConfigurationContext.Settings.Get<EndpointInstances>();
        }

        /// <summary>
        /// Returns the distribution policy.
        /// </summary>
        /// <param name="featureConfigurationContext">Context.</param>
        public static DistributionPolicy DistributionPolicy(this FeatureConfigurationContext featureConfigurationContext)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            return featureConfigurationContext.Settings.Get<DistributionPolicy>();
        }
    }
}