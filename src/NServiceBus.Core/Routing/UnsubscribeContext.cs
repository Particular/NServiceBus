namespace NServiceBus
{
    using System;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    class UnsubscribeContext : BehaviorContext, IUnsubscribeContext
    {
        public UnsubscribeContext(IBehaviorContext parentContext, Type eventType, ContextBag extensions, CancellationToken cancellationToken)
            : base(parentContext, cancellationToken)
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