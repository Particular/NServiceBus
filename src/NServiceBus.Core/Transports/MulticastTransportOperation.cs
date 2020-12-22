using NServiceBus.Transports;

namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Represents a transport operation which should be delivered to multiple receivers.
    /// </summary>
    public class MulticastTransportOperation : IOutgoingTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="MulticastTransportOperation" /> instance.
        /// </summary>
        public MulticastTransportOperation(OutgoingMessage message, Type messageType, TransportProperties properties, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default)
        {
            Message = message;
            MessageType = messageType;
            Properties = properties;
            RequiredDispatchConsistency = requiredDispatchConsistency;
        }

        /// <summary>
        /// Defines the message type which needs to be multicasted.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Properties that must be honored by the transport.
        /// </summary>
        /// <remarks>Properties should only ever be read. When there are no delivery constraints a cached empty constraints list is returned.</remarks>
        public TransportProperties Properties { get; }

        /// <summary>
        /// The message to be sent over the transport.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; }
    }
}