namespace NServiceBus;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains manually registered message types.
/// </summary>
class ManuallyRegisteredMessages
{
    public List<Type> MessageTypes { get; } = [];
}

