namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using Extensibility;

    /// <summary>
    /// Selects all instances.
    /// </summary>
    public class AllInstancesDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Returns a function that selects the next target of a message.
        /// </summary>
        public override Func<ContextBag, IEnumerable<UnicastRoutingTarget>> SelectDestination(List<UnicastRoutingTarget> allDestinations)
        {
            return bag => allDestinations;
        }
    }
}