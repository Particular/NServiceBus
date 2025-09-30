namespace NServiceBus;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains manually registered installer types.
/// </summary>
class ManuallyRegisteredInstallers
{
    public List<Type> InstallerTypes { get; } = [];
}

