namespace NServiceBus.Pipeline
{
    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public interface IOutgoingReplyContext : IOutgoingContext
    {
        /// <summary>
        /// The reply message.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }
}