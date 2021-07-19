namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public interface IIncomingPhysicalMessageContext : IIncomingContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        IncomingMessage Message { get; }
    }
}