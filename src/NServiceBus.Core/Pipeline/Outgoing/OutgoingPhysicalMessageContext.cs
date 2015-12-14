namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[].
    /// </summary>
    public class OutgoingPhysicalMessageContext : OutgoingContext, IOutgoingPhysicalMessageContext
    {
        /// <summary>
        /// Creates a new instance of an outgoing physical message context.
        /// </summary>
        /// <param name="body">The body of the message.</param>
        /// <param name="routingStrategies">The routing stragegies.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingPhysicalMessageContext(byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingLogicalMessageContext parentContext)
            : base(parentContext.MessageId, parentContext.Headers, parentContext)
        {
            Body = body;
            RoutingStrategies = routingStrategies;
        }

        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        /// <summary>
        /// A <see cref="byte"/> array containing the serialized contents of the outgoing message.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; } 
    }
}