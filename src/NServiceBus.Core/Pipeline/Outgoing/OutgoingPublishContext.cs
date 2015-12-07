namespace NServiceBus.OutgoingPipeline
{
    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public interface OutgoingPublishContext : OutgoingContext
    {
        /// <summary>
        /// The message to be published.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }
}