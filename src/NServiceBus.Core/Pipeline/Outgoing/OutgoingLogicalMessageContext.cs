namespace NServiceBus.Pipeline.Contexts
{
    using Extensibility;
    using OutgoingPipeline;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingLogicalMessageContext : BehaviorContext
    {
        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="OutgoingLogicalMessageContext"/>.
        /// </summary>
        /// <param name="message">The outgoing message.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingLogicalMessageContext(OutgoingLogicalMessage message, ContextBag parentContext)
            : base(parentContext)
        {
            Message = message;
            Set(message);
        }

        /// <summary>
        /// Updates the message instance.
        /// </summary>
        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull("newInstance", newInstance);

            Message = new OutgoingLogicalMessage(newInstance);
        }
    }
}