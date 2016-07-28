namespace NServiceBus.Transport
{
    using Extensibility;

    /// <summary>
    /// Represents a transaction used to receive the message from the queueing infrastructure.
    /// </summary>
    public sealed class TransportTransaction : ContextBag
    {
        /// <summary>
        /// Create an instance of <see cref="TransportTransaction" />.
        /// </summary>
        public TransportTransaction() : base(null)
        {
        }
    }
}