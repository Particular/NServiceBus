namespace NServiceBus;

using System.Collections.Concurrent;
using System.Collections.Generic;

// Pools Dictionary<string, string> instances used for per-message header copies. Clear() retains the
// internal Entry[]/buckets arrays, so reuse avoids the resize allocations that dominate header-copy traffic.
// Low-contention: one Interlocked fast-slot + a bounded ConcurrentStack overflow. Returned dictionaries
// are returned-clean (Clear()'d), so renters always get an empty dictionary to fill.
static class HeaderDictionaryPool
{
    const int MaxOverflow = 256;
    const int MaxRetainedCapacity = 1024; // don't retain outlier-sized dictionaries forever

    static Dictionary<string, string>? fastSlot;
    static readonly ConcurrentStack<Dictionary<string, string>> overflow = new();

    public static Dictionary<string, string> Get(IReadOnlyDictionary<string, string> source)
    {
        var d = TryGet() ?? new Dictionary<string, string>(source.Count > 0 ? source.Count : 4);
        foreach (var pair in source)
        {
            d[pair.Key] = pair.Value;
        }

        return d;
    }

    public static void Return(Dictionary<string, string> dictionary)
    {
        dictionary.Clear();

        if (dictionary.Capacity > MaxRetainedCapacity)
        {
            return;
        }

        if (System.Threading.Interlocked.CompareExchange(ref fastSlot, dictionary, null) is null)
        {
            return;
        }

        if (overflow.Count < MaxOverflow)
        {
            overflow.Push(dictionary);
        }
    }

    static Dictionary<string, string>? TryGet()
    {
        var d = System.Threading.Interlocked.Exchange(ref fastSlot, null);
        if (d is not null)
        {
            return d;
        }

        overflow.TryPop(out d);
        return d;
    }
}
