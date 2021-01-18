namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using Routing;

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
        /// <param name="properties">Delivery properties of the message.</param>
        public TransportOperation(OutgoingMessage message, AddressTag addressTag, OperationProperties properties = null, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default)
        {
            Message = message;
            AddressTag = addressTag;
            Properties = properties ?? new OperationProperties();
            RequiredDispatchConsistency = requiredDispatchConsistency;
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
        /// Operation properties.
        /// </summary>
        public OperationProperties Properties { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; set; }
    }
}