namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for publish operations
    /// </summary>
    public class OutgoingPublishContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with a parent context
        /// </summary>
        public OutgoingPublishContext(BehaviorContext parentContext, OutgoingLogicalMessage message, PublishOptions options)
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