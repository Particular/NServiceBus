namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[].
    /// </summary>
    public interface OutgoingPhysicalMessageContext : OutgoingContext
    {
        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        /// <summary>
        /// A <see cref="byte"/> array containing the serialized contents of the outgoing message.
        /// </summary>
        byte[] Body { get; set; }

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }
    }

    
    class OutgoingPhysicalMessageContextImpl : OutgoingContextImpl, OutgoingPhysicalMessageContext
    {
        public OutgoingPhysicalMessageContextImpl(byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, OutgoingLogicalMessageContext parentContext)
            : base(parentContext.MessageId, parentContext.Headers, parentContext)
        {
            Body = body;
            RoutingStrategies = routingStrategies;
        }

        public byte[] Body { get; set; }

        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; } 
    }
}