namespace NServiceBus
{
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public class PhysicalMessageProcessingContext : IncomingContext
    {
        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        public TransportMessage Message { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PhysicalMessageProcessingContext"/>.
        /// </summary>
        public PhysicalMessageProcessingContext(TransportMessage message, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = message;
        }
    }
}