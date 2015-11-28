namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public class OutgoingPublishContext : OutgoingContext
    {
        /// <summary>
        /// Initializes the context with a parent context.
        /// </summary>
        public OutgoingPublishContext(OutgoingLogicalMessage message, PublishOptions options, BehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
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