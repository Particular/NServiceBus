namespace NServiceBus.Routing
{
    using System;

    /// <summary>
    /// Implemented by routing distributors to provide their capabilities
    /// </summary>
    public abstract class DynamicRoutingDefinition
    {
        /// <summary>
        /// The feature to enable when this routing distributor is selected
        /// </summary>
        protected internal abstract Type ProvidedByFeature();
    }
}