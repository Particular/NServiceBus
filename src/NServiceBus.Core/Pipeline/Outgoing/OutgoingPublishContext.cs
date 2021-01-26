namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    class OutgoingPublishContext : OutgoingContext, IOutgoingPublishContext
    {
        public OutgoingPublishContext(OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, ContextBag extensions, IBehaviorContext parentContext, CancellationToken cancellationToken)
            : base(messageId, headers, parentContext, cancellationToken)
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