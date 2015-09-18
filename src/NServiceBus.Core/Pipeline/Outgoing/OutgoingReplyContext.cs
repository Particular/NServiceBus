namespace NServiceBus.OutgoingPipeline
{
    using Pipeline;

    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public class OutgoingReplyContext : BehaviorContext
    {
        /// <summary>
        /// The reply message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="OutgoingReplyContext"/>.
        /// </summary>
        public OutgoingReplyContext(OutgoingLogicalMessage message, ReplyOptions options, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = message;
            Guard.AgainstNull("parentContext", parentContext);
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("options", options);

            parentContext.Merge(options.Context);
        }
    }
}