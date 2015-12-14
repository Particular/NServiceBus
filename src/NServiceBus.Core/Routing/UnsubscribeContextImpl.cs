namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;

    class UnsubscribeContextImpl : BehaviorContextImpl, UnsubscribeContext
    {
        public UnsubscribeContextImpl(BehaviorContext parentContext, Type eventType, UnsubscribeOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}