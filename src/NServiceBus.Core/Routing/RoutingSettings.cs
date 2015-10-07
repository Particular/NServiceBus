namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

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
        public DirectRoutingTable DirectRoutingTable => GetOrCreate<DirectRoutingTable>();
        
        /// <summary>
        /// Gets the known endpoints collection.
        /// </summary>
        public KnownEndpoints KnownEndpoints => GetOrCreate<KnownEndpoints>();

        /// <summary>
        /// Gets the distribution policy.
        /// </summary>
        public DistributionPolicy DistributionPolicy => GetOrCreate<DistributionPolicy>();

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