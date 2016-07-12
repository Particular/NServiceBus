namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Represents a transport operation.
    /// </summary>
    public interface IOutgoingTransportOperation
    {
        /// <summary>
        /// The message to be sent over the transport.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// The delivery constraints that must be honored by the transport.
        /// </summary>
        List<DeliveryConstraint> DeliveryConstraints { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        DispatchConsistency RequiredDispatchConsistency { get; }
    }
}