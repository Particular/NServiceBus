namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Governs to how many and which instances of a given endpoint a message is to be sent.
    /// </summary>
    public abstract class DistributionStrategy
    {
        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public abstract IEnumerable<EndpointInstanceName> SelectDestination(IEnumerable<EndpointInstanceName> allInstances);
    }
}