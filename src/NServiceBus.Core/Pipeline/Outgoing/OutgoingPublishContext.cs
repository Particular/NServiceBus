namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    class OutgoingPublishContext : OutgoingContext, IOutgoingPublishContext
    {
        public OutgoingPublishContext(OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, PublishOptions options, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            Message = message;

            Merge(options.Context);
            MergeScoped(options.Context, messageId);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}