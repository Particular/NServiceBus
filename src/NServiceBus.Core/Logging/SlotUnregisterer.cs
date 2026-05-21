#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;

sealed class SlotUnregisterer(LogSlot slot) : IAsyncDisposable
{
    int unregistered;

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref unregistered, 1) == 0)
        {
            LogManager.UnregisterSlot(slot);
        }

        return ValueTask.CompletedTask;
    }
}