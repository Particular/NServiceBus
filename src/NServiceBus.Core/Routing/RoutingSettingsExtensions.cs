namespace NServiceBus
{
    using Routing;
    using Transports;

    /// <summary>
    /// Configuration extensions for routing.
    /// </summary>
    public static class RoutingSettingsExtensions
    {
        /// <summary>
        /// Configures the routing.
        /// </summary>
        public static RoutingSettings Routing(this TransportExtensions config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings(config.Settings);
        }

        /// <summary>
        /// Configures the routing.
        /// </summary>
        public static RoutingSettings<T> Routing<T>(this TransportExtensions<T> config)
            where T : TransportDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings<T>(config.Settings);
        }

        /// <summary>
        /// Sets a distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="config">Config object.</param>
        /// <param name="endpointName">The name of the logical endpoint the given strategy should apply to.</param>
        /// <param name="distributionStrategy">The instance of a distribution strategy.</param>
        public static void SetMessageDistributionStrategy<T>(this RoutingSettings<T> config, string endpointName, DistributionStrategy distributionStrategy)
            where T : TransportDefinition, INonCompetingConsumersTransport
        {
            config.Settings.GetOrCreate<DistributionPolicy>().SetDistributionStrategy(endpointName, distributionStrategy);
        }

        /// <summary>
        /// Configures physical routing.
        /// </summary>
        /// <param name="config">Config object.</param>
        public static EndpointInstances Physical<T>(this RoutingSettings<T> config)
            where T : TransportDefinition, INonCompetingConsumersTransport
        {
            return config.Settings.GetOrCreate<EndpointInstances>();
        }
    }
}