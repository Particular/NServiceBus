#nullable enable

namespace NServiceBus.Logging;

using System;

sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();
    public void Dispose() { }
}