namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Settings;

    /// <summary>
    /// Exposes advanced routing settings.
    /// </summary>
    public class RoutingMappingSettings : ExposeSettings
    {
        internal RoutingMappingSettings(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Gets the routing table for the direct routing.
        /// </summary>
        public UnicastRoutingTable Logical => GetOrCreate<UnicastRoutingTable>();

        /// <summary>
        /// Gets the known endpoints collection.
        /// </summary>
        public EndpointInstances Physical => GetOrCreate<EndpointInstances>();
        
        /// <summary>
        /// Sets a distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="endpointName">The name of the logical endpoint the given strategy should apply to.</param>
        /// <param name="distributionStrategy">The instance of a distribution strategy.</param>
        public void SetMessageDistributionStrategy(string endpointName, DistributionStrategy distributionStrategy)
        {
            GetOrCreate<DistributionPolicy>().SetDistributionStrategy(endpointName, distributionStrategy);
        }

        T GetOrCreate<T>()
            where T : new()
        {
            T value;
            if (!Settings.TryGet(out value))
            {
                value = new T();
                Settings.Set<T>(value);
            }
            return value;
        }
    }
}