namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Routing;

    /// <summary>
    /// File system base routing distribution.
    /// </summary>
    public class FileBasedRoutingDistributor : RoutingDistributorDefinition
    {
        /// <summary>
        /// The feature to enable when this routing distributor is selected
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(FileBasedRouterDistribution);
        }
    }
}