namespace NServiceBus.TransportDispatch
{
    using Routing;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Context for the dispatch part of the pipeline.
    /// </summary>
    public class DispatchContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public DispatchContext(OutgoingMessage messageToDispatch,RoutingStrategy routingStrategy, BehaviorContext context) : base(context)
        {
            Message = messageToDispatch;
            RoutingStrategy = routingStrategy;
        }

        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; private set; }

        /// <summary>
        /// The routing strategy for the operation to be dispatched.
        /// </summary>
        public RoutingStrategy RoutingStrategy { get; set; }
    }
}