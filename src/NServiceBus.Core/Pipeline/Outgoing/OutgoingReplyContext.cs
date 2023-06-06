namespace NServiceBus
{
    using System.Collections.Generic;
    using Extensibility;
    using Pipeline;

    class OutgoingReplyContext : OutgoingContext, IOutgoingReplyContext
    {
        public OutgoingReplyContext(OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, ContextBag extensions, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Guard.ThrowIfNull(parentContext);
            Guard.ThrowIfNull(message);
            Guard.ThrowIfNull(extensions);

            Message = message;

            Merge(extensions);
            Set(ExtendableOptions.OperationPropertiesKey, extensions);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}