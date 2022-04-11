namespace NServiceBus
{
    using System;
    using Pipeline;

    class SubscribeContext : BehaviorContext, ISubscribeContext
    {
        public SubscribeContext(IBehaviorContext parentContext, Type eventType, SubscribeOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            Merge(options.Context);
            MergeScoped(options.Context, "subscribe");

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}