namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Routing;

    /// <summary>
    /// Represents a transport operation which should be delivered to multiple receivers.
    /// </summary>
    public class MulticastTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="MulticastTransportOperation"/> instance.
        /// </summary>
        public MulticastTransportOperation(OutgoingMessage message, MulticastAddressTag addressTag, IEnumerable<DeliveryConstraint> deliveryConstraints = null, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default)
        {
            Message = message;
            AddressTag = addressTag;
            DeliveryConstraints = deliveryConstraints ?? Enumerable.Empty<DeliveryConstraint>();
            RequiredDispatchConsistency = requiredDispatchConsistency;
        }

        /// <summary>
        /// The message to be sent over the transport.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// Defines the receivers of the message.
        /// </summary>
        public MulticastAddressTag AddressTag { get; }

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