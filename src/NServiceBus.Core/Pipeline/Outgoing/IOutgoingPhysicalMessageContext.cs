namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a ReadOnlyMemory&gt;byte&lt;.
    /// </summary>
    public interface IOutgoingPhysicalMessageContext : IOutgoingContext
    {
        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        /// <summary>
        /// A <see cref="byte" /> array containing the serialized contents of the outgoing message.
        /// </summary>
        ReadOnlyMemory<byte> Body { get; }

        /// <summary>
        /// The routing strategies for this message.
        /// </summary>
        IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }

        /// <summary>
        /// Updates the message with the given body.
        /// </summary>
        void UpdateMessage(ReadOnlyMemory<byte> body);
    }
}