namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class SatelliteProcessingContext : BehaviorContext, ISatelliteProcessingContext
    {
        public SatelliteProcessingContext(IncomingMessage message, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Message = message;
        }

        public IncomingMessage Message { get; }
    }
}