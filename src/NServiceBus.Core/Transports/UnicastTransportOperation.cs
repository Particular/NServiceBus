namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Routing;

    /// <summary>
    /// Represents a transport operation which should be delivered to a single receiver.
    /// </summary>
    public class UnicastTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="UnicastTransportOperation"/> instance.
        /// </summary>
        public UnicastTransportOperation(OutgoingMessage message, UnicastAddressTag addressTag, IEnumerable<DeliveryConstraint> deliveryConstraints = null, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default)
        {
            Message = message;
            AddressTag = addressTag;
            DeliveryConstraints = deliveryConstraints ?? Enumerable.Empty<DeliveryConstraint>();
            RequiredDispatchConsistency = requiredDispatchConsistency;
        }

        /// <summary>
        /// The message to be sent over the transport.
        /// </summary>
        public OutgoingMessage Message { get; set; }

        /// <summary>
        /// Defines the receiver of the message.
        /// </summary>
        public UnicastAddressTag AddressTag { get; set; }

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