namespace NServiceBus
{
    using System.Collections.Generic;
    using Extensibility;
    using Pipeline;

    class OutgoingSendContext : OutgoingContext, IOutgoingSendContext
    {
        public OutgoingSendContext(OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, ContextBag extensinos, IBehaviorContext parentContext)
            : base(messageId, headers, parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(extensinos), extensinos);

            Message = message;

            Merge(extensinos);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}