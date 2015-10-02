namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Routing;

    /// <summary>
    /// Contains details on how the message should be sent.
    /// </summary>
    public class DispatchOptions
    {
        /// <summary>
        /// Creates the send options with the given routing strategy.
        /// </summary>
        /// <param name="addressLabel">The address to use when routing this message.</param>
        /// <param name="requiredDispatchConsistency">The required consistency level for the dispatch operation.</param>
        /// <param name="deliveryConstraints">The delivery constraints that must be honored by the transport.</param>
        public DispatchOptions(AddressLabel addressLabel, DispatchConsistency requiredDispatchConsistency, IEnumerable<DeliveryConstraint> deliveryConstraints = null)
        {
            AddressLabel = addressLabel;
            RequiredDispatchConsistency = requiredDispatchConsistency;

            if (deliveryConstraints != null)
            {
                DeliveryConstraints = deliveryConstraints;
            }
            else
            {
                DeliveryConstraints = new List<DeliveryConstraint>();
            }
        }

        /// <summary>
        /// The strategy to use when routing this message.
        /// </summary>
        public AddressLabel AddressLabel { get; private set; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        public IEnumerable<DeliveryConstraint> DeliveryConstraints { get; private set; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; set; }
    }
}