namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using ReplyOptions = NServiceBus.ReplyOptions;

    /// <summary>
    /// Pipeline context for reply operations.
    /// </summary>
    public class OutgoingReplyContext : OutgoingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OutgoingReplyContext" />.
        /// </summary>
        public OutgoingReplyContext(OutgoingLogicalMessage message, ReplyOptions options, BehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Merge(options.Context);
        }

        /// <summary>
        /// The reply message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; private set; }
    }
}