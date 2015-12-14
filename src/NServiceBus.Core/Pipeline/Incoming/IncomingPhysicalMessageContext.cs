namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public class IncomingPhysicalMessageContext : IncomingContext, IIncomingPhysicalMessageContext
    {
        /// <summary>
        /// Creates a new instance of an incoming phyiscal message context.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="parentContext">The parent context.</param>
        public IncomingPhysicalMessageContext(IncomingMessage message, IBehaviorContext parentContext)
            : base(message.MessageId, message.GetReplyToAddress(), message.Headers, parentContext)
        {
            Message = message;
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; }
    }
}