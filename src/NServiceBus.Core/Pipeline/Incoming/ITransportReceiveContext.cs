namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public interface ITransportReceiveContext : IBehaviorContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        IncomingMessage Message { get; }

        /// <summary>
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// </summary>
        void AbortReceiveOperation();
    }
}