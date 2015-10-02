namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using Routing;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Context for the dispatch part of the pipeline.
    /// </summary>
    public class RoutingContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public RoutingContext(OutgoingMessage messageToDispatch, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext context) : base(context)
        {
            Message = messageToDispatch;
            RoutingStrategies = routingStrategies;
        }

        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public RoutingContext(OutgoingMessage messageToDispatch, RoutingStrategy addressLabel, BehaviorContext context) : base(context)
        {
            Message = messageToDispatch;
            RoutingStrategies = new [] {addressLabel};
        }

        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; private set; }

        /// <summary>
        /// The routing strategies for the operation to be dispatched.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }
    }
}