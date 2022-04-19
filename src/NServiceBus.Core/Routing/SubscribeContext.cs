namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using DeliveryConstraints;
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
            Set(ExtendableOptions.OperationPropertiesKey, extensions);
            Set(new List<DeliveryConstraint>(0)); // set empty delivery constraint to prevent leaking nested constraints collections.

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}