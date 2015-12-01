namespace NServiceBus
{
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public interface PhysicalMessageProcessingContext : IncomingContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        IncomingMessage Message { get; }
    }
}