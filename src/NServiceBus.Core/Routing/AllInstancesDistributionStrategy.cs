namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Selects all instances.
    /// </summary>
    public class AllInstancesDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public override IEnumerable<UnicastRoutingTarget> SelectDestination(IEnumerable<UnicastRoutingTarget> allInstances)
        {
            return allInstances;
        }
    }
}