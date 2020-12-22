using NServiceBus.Transports;

namespace NServiceBus.Transport
{

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
        /// The delivery properties that must be honored by the transport.
        /// </summary>
        TransportProperties Properties { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        DispatchConsistency RequiredDispatchConsistency { get; }
    }
}