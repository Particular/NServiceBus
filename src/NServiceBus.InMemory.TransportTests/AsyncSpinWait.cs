namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;

static class AsyncSpinWait
{
    public static async Task Until(Func<bool> condition, int maxIterations = 1000, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < maxIterations && !condition(); i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }
}
