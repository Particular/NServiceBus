namespace NServiceBus;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains manually registered handler types.
/// </summary>
class ManuallyRegisteredHandlers
{
    public List<Type> HandlerTypes { get; } = [];
}
