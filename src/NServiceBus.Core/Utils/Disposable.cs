#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;

static class Disposable
{
    public static IAsyncDisposable Wrap(object? maybeDisposable) =>
        maybeDisposable switch
        {
            null => NoOpDisposable.Instance,
            IAsyncDisposable asyncDisposable => asyncDisposable,
            IDisposable disposable => new SyncDisposable(disposable),
            _ => NoOpDisposable.Instance
        };

    sealed class NoOpDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public static readonly NoOpDisposable Instance = new();
    }

    sealed class SyncDisposable(IDisposable disposable) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            disposable.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}