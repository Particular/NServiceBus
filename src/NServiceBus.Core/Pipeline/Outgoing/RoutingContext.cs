namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Transports;
    using Routing;
    using Pipeline;

    /// <summary>
    /// Context for the dispatch part of the pipeline.
    /// </summary>
    public class RoutingContext : OutgoingContext
    {
        internal RoutingContext(OutgoingMessage message, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext context)
            : this(message.MessageId, message.Headers, message.Body, routingStrategies, context)
        {
        }

        internal RoutingContext(OutgoingMessage message, RoutingStrategy addressLabel, BehaviorContext context)
            : this(message.MessageId, message.Headers, message.Body, new[] { addressLabel }, context)
        {
        }

        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public RoutingContext(string messageId, Dictionary<string, string> headers, byte[] body, RoutingStrategy addressLabel, BehaviorContext context)
            : this(messageId, headers, body, new[] { addressLabel }, context)
        {
        }

        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public RoutingContext(string messageId, Dictionary<string, string> headers, byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, BehaviorContext context) 
            : base(messageId, headers, context)
        {
            RoutingStrategies = routingStrategies;
            Body = body;
        }

        /// <summary>
        /// The routing strategies for the operation to be dispatched.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }

        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        public byte[] Body { get; }
    }
}