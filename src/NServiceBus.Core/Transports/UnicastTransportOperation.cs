using NServiceBus.Transports;

namespace NServiceBus.Transport
{
    /// <summary>
    /// Represents a transport operation which should be delivered to a single receiver.
    /// </summary>
    public class UnicastTransportOperation : IOutgoingTransportOperation
    {
        /// <summary>
        /// Creates a new <see cref="UnicastTransportOperation" /> instance.
        /// </summary>
        public UnicastTransportOperation(OutgoingMessage message, string destination, TransportProperties properties, DispatchConsistency requiredDispatchConsistency = DispatchConsistency.Default)
        {
            Message = message;
            Destination = destination;
            Properties = properties;
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
        /// Properties constraints that must be honored by the transport.
        /// </summary>
        public TransportProperties Properties { get; }

        /// <summary>
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        public DispatchConsistency RequiredDispatchConsistency { get; }
    }
}