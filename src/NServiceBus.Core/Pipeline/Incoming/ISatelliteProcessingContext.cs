namespace NServiceBus.Pipeline
{
    using NServiceBus.Transports;

    /// <summary>
    /// A context of satellite behavior execution.
    /// </summary>
    public interface ISatelliteProcessingContext : IBehaviorContext
    {
        /// <summary>
        /// The physical message being processed.
        /// </summary>
        IncomingMessage Message { get; }
    }
}