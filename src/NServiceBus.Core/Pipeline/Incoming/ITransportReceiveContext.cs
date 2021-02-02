namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public partial interface ITransportReceiveContext : IBehaviorContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        IncomingMessage Message { get; }
    }
}