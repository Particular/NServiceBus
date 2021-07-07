﻿namespace NServiceBus
{
    using Extensibility;
    using Pipeline;

    class OutgoingReplyContext : OutgoingContext, IOutgoingReplyContext
    {
        public OutgoingReplyContext(OutgoingLogicalMessage message, string messageId, HeaderDictionary headers, ContextBag extensions, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(extensions), extensions);

            Message = message;

            Merge(extensions);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}