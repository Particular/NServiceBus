namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public class OutgoingReplyContext : BehaviorContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OutgoingReplyContext"/>.
        /// </summary>
        public OutgoingReplyContext(BehaviorContext parentContext, OutgoingLogicalMessage message, ReplyOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(parentContext, "parentContext");
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(options, "options");

            Set(message);

            parentContext.Merge(options.Context);
        }
    }
}