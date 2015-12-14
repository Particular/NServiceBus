namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for publish operations.
    /// </summary>
    public class OutgoingPublishContext : OutgoingContext, IOutgoingPublishContext
    {
        /// <summary>
        /// Creates a new instance of an outgoing publish context.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="options">The publish options.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingPublishContext(OutgoingLogicalMessage message, PublishOptions options, IBehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);
        }

        /// <summary>
        /// The message to be published.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }
    }
}