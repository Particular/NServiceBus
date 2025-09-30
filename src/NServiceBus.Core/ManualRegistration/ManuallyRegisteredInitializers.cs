namespace NServiceBus;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains manually registered initializer types.
/// </summary>
class ManuallyRegisteredInitializers
{
    public List<Type> InitializerTypes { get; } = [];
}

