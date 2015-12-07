namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using Routing;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Context for the dispatch part of the pipeline.
    /// </summary>
    public interface RoutingContext : BehaviorContext
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