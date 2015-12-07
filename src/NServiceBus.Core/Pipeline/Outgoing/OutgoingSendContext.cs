namespace NServiceBus.OutgoingPipeline
{
    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    public interface OutgoingSendContext : OutgoingContext
    {
        /// <summary>
        /// The message being sent.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }
}