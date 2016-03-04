namespace NServiceBus
{
    using System;
    using Pipeline;

    class UnsubscribeContext : BehaviorContext, IUnsubscribeContext
    {
        public UnsubscribeContext(IBehaviorContext parentContext, Type eventType, UnsubscribeOptions options)
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