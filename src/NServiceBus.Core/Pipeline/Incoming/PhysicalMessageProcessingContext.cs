namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public class PhysicalMessageProcessingContext : IncomingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PhysicalMessageProcessingContext" />.
        /// </summary>
        public PhysicalMessageProcessingContext(IncomingMessage message, BehaviorContext parentContext)
            : base(message.MessageId, message.GetReplyToAddress(), message.Headers, parentContext)
        {
            Message = message;
        }

        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        public IncomingMessage Message { get; private set; }
    }
}