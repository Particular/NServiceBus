namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    class OutgoingPublishContext : OutgoingContext, IOutgoingPublishContext
    {
        public OutgoingPublishContext(OutgoingLogicalMessage message, PublishOptions options, IBehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}