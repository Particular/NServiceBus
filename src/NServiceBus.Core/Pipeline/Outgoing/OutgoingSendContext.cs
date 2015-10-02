namespace NServiceBus.OutgoingPipeline
{
    using Pipeline;

    /// <summary>
    /// Pipeline context for send operations.
    /// </summary>
    public class OutgoingSendContext : BehaviorContext
    {
        /// <summary>
        /// The message beeing sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; }

        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingSendContext(OutgoingLogicalMessage message, SendOptions options, BehaviorContext parentContext)
            : base(parentContext)
        {
            Guard.AgainstNull("parentContext", parentContext);
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("options", options);

            Message = message;

            Merge(options.Context);
        }
    }
}