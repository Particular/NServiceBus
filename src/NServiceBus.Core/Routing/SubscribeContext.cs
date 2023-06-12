namespace NServiceBus
{
    using System;
    using System.ComponentModel;
    using Extensibility;
    using Pipeline;

    class SubscribeContext : BehaviorContext, ISubscribeContext
    {
        public SubscribeContext(IBehaviorContext parentContext, Type[] eventTypes, ContextBag extensions)
            : base(parentContext)
        {
            Guard.ThrowIfNull(parentContext);
            Guard.ThrowIfNull(eventTypes);
            Guard.ThrowIfNull(extensions);

            Merge(extensions);
            Set(ExtendableOptions.OperationPropertiesKey, extensions);

            EventTypes = eventTypes;
        }

        public Type[] EventTypes { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Type EventType => throw new NotImplementedException();
    }
}