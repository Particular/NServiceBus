namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
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
        /// Gets the publisher settings.
        /// </summary>
        public Publishers Publishers => GetOrCreate<Publishers>();

        /// <summary>
        /// Sets a distribution strategy for a given subset of message types.
        /// </summary>
        /// <param name="distributionStrategy">The instance of a distribution strategy.</param>
        /// <param name="typeMatchingRule">A predicate for determining the set of types.</param>
        public void SetMessageDistributionStrategy(DistributionStrategy distributionStrategy, Func<Type, bool> typeMatchingRule)
        {
            GetOrCreate<DistributionPolicy>().SetDistributionStrategy(distributionStrategy, typeMatchingRule);
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