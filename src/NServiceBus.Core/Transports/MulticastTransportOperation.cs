namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DeliveryConstraints;

    /// <summary>
    /// Represents a transport operation which should be delivered to multiple receivers.
    /// </summary>
    public class MulticastTransportOperation : IOutgoingTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="MulticastTransportOperation" /> instance.
        /// </summary>
        public MulticastTransportOperation(OutgoingMessage message, Type messageType, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default, IEnumerable<DeliveryConstraint> deliveryConstraints = null)
        {
            Message = message;
            MessageType = messageType;
            DeliveryConstraints = deliveryConstraints ?? Enumerable.Empty<DeliveryConstraint>();
            RequiredDispatchConsistency = requiredDispatchConsistency;
        }

        /// <summary>
        /// Defines the message type which needs to be multicasted.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// The message to be sent over the transport.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        public IEnumerable<DeliveryConstraint> DeliveryConstraints { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; }
    }
}