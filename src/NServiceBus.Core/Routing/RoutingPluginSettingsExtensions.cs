namespace NServiceBus
{
    using Features;
    using Routing;
    using Routing.MessageDrivenSubscriptions;

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
        /// Returns the publishers table. This API is only used when running a transport that does not have a native Publish-Subscribe support. Refer to the documentation for more information
        /// about native and emulated Publish-Subscribe.
        /// </summary>
        /// <param name="featureConfigurationContext">Context.</param>
        public static Publishers Publishers(this FeatureConfigurationContext featureConfigurationContext)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            return featureConfigurationContext.Settings.Get<Publishers>();
        }

        /// <summary>
        /// Returns the routing table. This API is only used when running a transport that does not have native scale-out support via Competing Consumers pattern. Refer to the documentation for 
        /// more information about scaling out.
        /// </summary>
        /// <param name="featureConfigurationContext">Context.</param>
        public static EndpointInstances EndpointInstances(this FeatureConfigurationContext featureConfigurationContext)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            return featureConfigurationContext.Settings.Get<EndpointInstances>();
        }

        /// <summary>
        /// Returns the distribution policy. This API is only used when running a transport that does not have native scale-out support via Competing Consumers pattern. Refer to the documentation for 
        /// more information about scaling out.
        /// </summary>
        /// <param name="featureConfigurationContext">Context.</param>
        public static DistributionPolicy DistributionPolicy(this FeatureConfigurationContext featureConfigurationContext)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            return featureConfigurationContext.Settings.Get<DistributionPolicy>();
        }
    }
}