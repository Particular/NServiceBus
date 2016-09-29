namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Represents a transport operation which should be delivered to multiple receivers.
    /// </summary>
    public class MulticastTransportOperation : IOutgoingTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="MulticastTransportOperation" /> instance.
        /// </summary>
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        public MulticastTransportOperation(OutgoingMessage message, Type messageType, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default, List<DeliveryConstraint> deliveryConstraints = null)
        {
            Message = message;
            MessageType = messageType;
            DeliveryConstraints = deliveryConstraints ?? DeliveryConstraint.EmptyConstraints;
            RequiredDispatchConsistency = requiredDispatchConsistency;
        }

        /// <summary>
        /// Defines the message type which needs to be multicasted.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        /// <remarks>The delivery constraints should only ever be read. When there are no delivery constraints a cached empty constraints list is returned.</remarks>
        public List<DeliveryConstraint> DeliveryConstraints { get; }

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