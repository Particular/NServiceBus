namespace NServiceBus
{
    using System;
    using Extensibility;
    using Pipeline;

    class UnsubscribeContext : BehaviorContext, IUnsubscribeContext
    {
        public UnsubscribeContext(IBehaviorContext parentContext, Type eventType, ContextBag extensions)
            : base(parentContext)
        {
            Guard.ThrowIfNull(parentContext);
            Guard.ThrowIfNull(eventType);
            Guard.ThrowIfNull(extensions);

            Merge(extensions);
            Set(ExtendableOptions.OperationPropertiesKey, extensions);

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}