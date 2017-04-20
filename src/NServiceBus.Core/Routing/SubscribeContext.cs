namespace NServiceBus
{
    using System;
    using Extensibility;
    using Pipeline;

    class SubscribeContext : BehaviorContext, ISubscribeContext
    {
        public SubscribeContext(IBehaviorContext parentContext, Type eventType, ContextBag extensions)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(extensions), extensions);

            Merge(extensions);

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}