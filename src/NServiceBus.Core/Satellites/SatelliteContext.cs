namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Context for the satellite part of the pipeline.
    /// </summary>
    public class SatelliteContext : TransportReceiveContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        public SatelliteContext(BehaviorContext context) : base(context)
        {
        }
    }
}