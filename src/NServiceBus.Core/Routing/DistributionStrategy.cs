namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using Extensibility;

    /// <summary>
    /// Governs to how many and which instances of a given endpoint a message is to be sent.
    /// </summary>
    public abstract class DistributionStrategy
    {
        /// <summary>
        /// Returns a function that selects the next target of a message.
        /// </summary>
        public abstract Func<ContextBag, IEnumerable<UnicastRoutingTarget>> SelectDestination(List<UnicastRoutingTarget> allDestinations);
    }
}