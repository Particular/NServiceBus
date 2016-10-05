namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Represents a transport operation which should be delivered to a single receiver.
    /// </summary>
    public class UnicastTransportOperation : IOutgoingTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="UnicastTransportOperation" /> instance.
        /// </summary>
        public UnicastTransportOperation(OutgoingMessage message, string destination, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default, List<DeliveryConstraint> deliveryConstraints = null)
        {
            Message = message;
            Destination = destination;
            DeliveryConstraints = deliveryConstraints ?? DeliveryConstraint.EmptyConstraints;
            RequiredDispatchConsistency = requiredDispatchConsistency;
        }

        /// <summary>
        /// Defines the destination address of the receiver.
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// The message to be sent over the transport.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        public List<DeliveryConstraint> DeliveryConstraints { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; }
    }
}