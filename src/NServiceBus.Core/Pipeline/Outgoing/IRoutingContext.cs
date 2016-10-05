namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Routing;
    using Transport;

    /// <summary>
    /// Context for the routing part of the pipeline.
    /// </summary>
    public interface IRoutingContext : IBehaviorContext
    {
        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// The routing strategies for the operation to be dispatched.
        /// </summary>
        IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }
    }
}