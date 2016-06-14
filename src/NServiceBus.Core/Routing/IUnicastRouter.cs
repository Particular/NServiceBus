namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Represents a routing behavior.
    /// </summary>
    public interface IUnicastRouter
    {
        /// <summary>
        /// Determines the destinations for a given message type.
        /// </summary>
        /// <param name="messageType">Type of message.</param>
        /// <param name="distributionPolicy">Distribution policy.</param>
        /// <param name="contextBag">Context.</param>
        Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, Func<string, DistributionStrategy> distributionPolicy, ContextBag contextBag);
    }
}