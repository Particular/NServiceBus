namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public class OutgoingPublishContext : BehaviorContext
    {
        /// <summary>
        /// The message to be published.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }

        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingPublishContext(OutgoingLogicalMessage message, PublishOptions options, BehaviorContext parentContext)
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