namespace NServiceBus;

using System;
using Extensibility;
using Pipeline;

class SubscribeContext : BehaviorContext, ISubscribeContext
{
    public SubscribeContext(IBehaviorContext parentContext, Type[] eventTypes, ContextBag extensions)
        : base(parentContext)
    {
        ArgumentNullException.ThrowIfNull(parentContext);
        ArgumentNullException.ThrowIfNull(eventTypes);
        ArgumentNullException.ThrowIfNull(extensions);

        Merge(extensions);
        Set(ExtendableOptions.OperationPropertiesKey, extensions);

        EventTypes = eventTypes;
    }

    public Type[] EventTypes { get; }
}