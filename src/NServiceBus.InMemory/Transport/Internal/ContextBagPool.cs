namespace NServiceBus;

using System.Collections.Concurrent;
using System.Threading;
using Extensibility;

// EXPERIMENTAL: pools ContextBag instances for the per-message processing context. Reset() retains the
// internal stash Dictionary's capacity, so reuse avoids the resize allocations that dominate the ContextBag cluster.
static class ContextBagPool
{
    const int MaxOverflow = 256;

    static ContextBag? fastSlot;
    static readonly ConcurrentStack<ContextBag> overflow = new();

    public static ContextBag Get()
    {
        var bag = TryGet() ?? new ContextBag();
        return bag;
    }

    public static void Return(ContextBag bag)
    {
        bag.Reset();

        if (Interlocked.CompareExchange(ref fastSlot, bag, null) is null)
        {
            return;
        }

        if (overflow.Count < MaxOverflow)
        {
            overflow.Push(bag);
        }
    }

    static ContextBag? TryGet()
    {
        var bag = Interlocked.Exchange(ref fastSlot, null);
        if (bag is not null)
        {
            return bag;
        }

        overflow.TryPop(out bag);
        return bag;
    }
}
