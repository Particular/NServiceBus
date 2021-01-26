namespace NServiceBus
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    class SubscribeContext : BehaviorContext, ISubscribeContext
    {
        public SubscribeContext(IBehaviorContext parentContext, Type[] eventTypes, ContextBag extensions, CancellationToken cancellationToken)
            : base(parentContext, cancellationToken)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventTypes), eventTypes);
            Guard.AgainstNull(nameof(extensions), extensions);

            Merge(extensions);

            EventTypes = eventTypes;
        }

        public Type[] EventTypes { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Type EventType => throw new NotImplementedException();
    }
}