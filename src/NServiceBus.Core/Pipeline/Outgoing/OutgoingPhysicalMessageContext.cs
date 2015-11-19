namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[].
    /// </summary>
    public interface OutgoingPhysicalMessageContext : IOutgoingContext
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

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[].
    /// </summary>
    public class OutgoingPhysicalMessageContextImpl : OutgoingContext, OutgoingPhysicalMessageContext
    {

        internal OutgoingPhysicalMessageContextImpl(byte[] body, OutgoingLogicalMessageContext parentContext) 
            : this(parentContext.MessageId, parentContext.Headers, body, parentContext.RoutingStrategies, parentContext)
        {
        }

        /// <summary>
        /// Initializes an instance of the context.
        /// </summary>
        public OutgoingPhysicalMessageContextImpl(string messageId, Dictionary<string, string> headers, byte[] body, IReadOnlyCollection<RoutingStrategy> routingStrategies, OutgoingLogicalMessageContext parentContext)
            : base(messageId, headers, parentContext)
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