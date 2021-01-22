namespace NServiceBus
{
    using System;
    using Extensibility;
    using Pipeline;

    class SubscribeContext : BehaviorContext, ISubscribeContext
    {
        public SubscribeContext(IBehaviorContext parentContext, Type[] eventTypes, ContextBag extensions)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventTypes), eventTypes);
            Guard.AgainstNull(nameof(extensions), extensions);

            Merge(extensions);

            EventTypes = eventTypes;
        }

        public Type[] EventTypes { get; }
    }
}