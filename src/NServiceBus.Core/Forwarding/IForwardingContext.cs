namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// Provide context to behaviors on the forwarding pipeline.
    /// </summary>
    public interface IForwardingContext : IBehaviorContext
    {
        /// <summary>
        /// The message to be forwarded.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// The address of the forwarding queue.
        /// </summary>
        string Address { get; }
    }
}