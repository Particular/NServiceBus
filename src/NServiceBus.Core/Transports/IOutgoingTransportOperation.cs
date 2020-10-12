namespace NServiceBus.Transport
{
    using System.Collections.Generic;

    ////TODO: should we make this a base class instead of an interface?

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
        /// The dispatch consistency the must be honored by the transport.
        /// </summary>
        DispatchConsistency RequiredDispatchConsistency { get; }

        /// <summary>
        /// 
        /// </summary>
        Dictionary<string, string> Properties { get; }
    }
}