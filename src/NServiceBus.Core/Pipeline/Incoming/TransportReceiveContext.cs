namespace NServiceBus.Pipeline.Contexts
{
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public interface TransportReceiveContext : BehaviorContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        IncomingMessage Message { get; }
    }
}