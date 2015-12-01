namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    class OutgoingSendContextImpl : OutgoingContextImpl, OutgoingSendContext
    {
        public OutgoingSendContextImpl(OutgoingLogicalMessage message, SendOptions options, BehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            Message = message;

            Merge(options.Context);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}