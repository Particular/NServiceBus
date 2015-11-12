namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;
    using PublishOptions = NServiceBus.PublishOptions;

    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public class OutgoingPublishContext : OutgoingContext
    {
        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingPublishContext(OutgoingLogicalMessage message, PublishOptions options, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Merge(options.Context);
        }

        /// <summary>
        /// The message to be published.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }
    }
}