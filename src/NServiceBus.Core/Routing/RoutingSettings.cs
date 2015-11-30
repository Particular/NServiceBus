namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings : ExposeSettings
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Gets the routing table for the direct routing.
        /// </summary>
        public UnicastRoutingTable UnicastRoutingTable => GetOrCreate<UnicastRoutingTable>();
        
        /// <summary>
        /// Gets the known endpoints collection.
        /// </summary>
        public EndpointInstances EndpointInstances => GetOrCreate<EndpointInstances>();

        /// <summary>
        /// Sets a distribution strategy for a given subset of message types.
        /// </summary>
        /// <param name="distributionStrategy">The instance of a distribution strategy.</param>
        /// <param name="typeMatchingRule">A predicate for determining the set of types.</param>
        public void SetMessageDistributionStrategy(DistributionStrategy distributionStrategy, Func<Type, bool> typeMatchingRule)
        {
            GetOrCreate<DistributionPolicy>().SetDistributionStrategy(distributionStrategy, typeMatchingRule);
        }

        /// <summary>
        /// Gets the transport addresses.
        /// </summary>
        public TransportAddresses TransportAddresses => GetOrCreate<TransportAddresses>();

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