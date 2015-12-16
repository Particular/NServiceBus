namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Routing;

    /// <summary>
    /// Defines the transport operations including the message and information how to send it.
    /// </summary>
    public class TransportOperation
    {
        /// <summary>
        /// Creates a new transport operation.
        /// </summary>
        /// <param name="message">The message to dispatch.</param>
        /// <param name="addressTag">The address to use when routing this message.</param>
        /// <param name="requiredDispatchConsistency">The required consistency level for the dispatch operation.</param>
        /// <param name="deliveryConstraints">The delivery constraints that must be honored by the transport.</param>
        public TransportOperation(OutgoingMessage message, AddressTag addressTag, DispatchConsistency requiredDispatchConsistency, IEnumerable<DeliveryConstraint> deliveryConstraints = null)
        {
            Message = message;
            AddressTag = addressTag;
            RequiredDispatchConsistency = requiredDispatchConsistency;
            DeliveryConstraints = deliveryConstraints ?? Enumerable.Empty<DeliveryConstraint>();
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// The strategy to use when routing this message.
        /// </summary>
        public AddressTag AddressTag { get; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        public IEnumerable<DeliveryConstraint> DeliveryConstraints { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; set; }
    }
}