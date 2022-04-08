namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    class OutgoingReplyContext : OutgoingContext, IOutgoingReplyContext
    {
        public OutgoingReplyContext(OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, ReplyOptions options, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            Message = message;

            Merge(options.Context);
            Merge(options.Context, messageId);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}