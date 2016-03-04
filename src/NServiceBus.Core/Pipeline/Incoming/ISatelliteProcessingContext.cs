namespace NServiceBus.Pipeline
{
    using Transports;

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