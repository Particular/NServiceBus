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

            Merge(options.Context);
            MergeScoped(options.Context, "unsubscribe");

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}