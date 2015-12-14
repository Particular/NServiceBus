namespace NServiceBus.OutgoingPipeline
{
    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    public interface IOutgoingSendContext : IOutgoingContext
    {
        /// <summary>
        /// The message being sent.
        /// </summary>
        OutgoingLogicalMessage Message { get; }
    }
}