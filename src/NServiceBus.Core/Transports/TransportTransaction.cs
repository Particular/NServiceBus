namespace NServiceBus.Transport
{
    using Extensibility;

    /// <summary>
    /// Represents a transaction used to receive the message from the queueing infrastructure.
    /// </summary>
    public class TransportTransaction : ContextBag
    {
        /// <summary>
        /// Create an instance of <see cref="TransportTransaction" />.
        /// </summary>
        public TransportTransaction() : base(null)
        {
        }
    }
}