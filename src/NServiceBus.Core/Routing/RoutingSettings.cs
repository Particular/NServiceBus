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
            settings.Set<DirectRoutingTable>(new DirectRoutingTable());
            settings.Set<KnownEndpoints>(new KnownEndpoints());
            settings.Set<DistributionPolicy>(new DistributionPolicy());
        }

        /// <summary>
        /// Gets the routing table for the direct routing.
        /// </summary>
        public DirectRoutingTable DirectRoutingTable
        {
            get { return Settings.Get<DirectRoutingTable>(); }
        }

        /// <summary>
        /// Gets the known endpoints collection.
        /// </summary>
        public KnownEndpoints KnownEndpoints
        {
            get { return Settings.Get<KnownEndpoints>(); }
        }
        
        /// <summary>
        /// Gets the distribution policy.
        /// </summary>
        public DistributionPolicy DistributionPolicy
        {
            get { return Settings.Get<DistributionPolicy>(); }
        }

    }
}