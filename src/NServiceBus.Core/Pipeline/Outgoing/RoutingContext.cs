namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    /// <summary>
    /// Context for the routing part of the pipeline.
    /// </summary>
    public class RoutingContext : OutgoingContext, IRoutingContext
    {
        /// <summary>
        /// Creates a new instance of a routing parentContext.
        /// </summary>
        /// <param name="messageToDispatch">The message to dispatch.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <param name="parentContext">The parent context.</param>
        public RoutingContext(OutgoingMessage messageToDispatch, RoutingStrategy routingStrategy, IBehaviorContext parentContext)
            : this(messageToDispatch, new[] { routingStrategy }, parentContext)
        {
        }

        /// <summary>
        /// Creates a new instance of a routing parentContext.
        /// </summary>
        /// <param name="messageToDispatch">The message to dispatch.</param>
        /// <param name="routingStrategies">The routing strategies.</param>
        /// <param name="parentContext">The parent context.</param>
        public RoutingContext(OutgoingMessage messageToDispatch, IReadOnlyCollection<RoutingStrategy> routingStrategies, IBehaviorContext parentContext)
            : base(messageToDispatch.MessageId, messageToDispatch.Headers, parentContext)
        {
            Message = messageToDispatch;
            RoutingStrategies = routingStrategies;
        }

        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// The routing strategies for the operation to be dispatched.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }
    }
}