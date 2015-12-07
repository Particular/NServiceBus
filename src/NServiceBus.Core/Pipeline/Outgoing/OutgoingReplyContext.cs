namespace NServiceBus.OutgoingPipeline
{
    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public interface OutgoingReplyContext : OutgoingContext
    {
        /// <summary>
        /// The reply message.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }
}