#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;

sealed class NoOpAsyncDisposable : IAsyncDisposable
{
    public static readonly NoOpAsyncDisposable Instance = new();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
