namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Routing;

    /// <summary>
    /// Provides support for round robin scaleout using a static list of endpoints in a text file
    /// </summary>
    public class FileBasedRoundRobinDistribution : DynamicRoutingDefinition
    {
        /// <summary>
        /// The feature to enable when this routing distributor is selected
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(FileBasedRoundRobinDynamicRouting);
        }
    }
}