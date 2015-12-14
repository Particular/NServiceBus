namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public class OutgoingReplyContext : OutgoingContext, IOutgoingReplyContext
    {
        /// <summary>
        /// Creates a new instance of an outgoing reply context.
        /// </summary>
        /// <param name="message">The reply message.</param>
        /// <param name="options">The options.</param>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingReplyContext(OutgoingLogicalMessage message, ReplyOptions options, IBehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);
        }

        /// <summary>
        /// The reply message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }
    }
}