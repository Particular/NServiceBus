#nullable enable

namespace NServiceBus.Utils;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// A thread-safe pool of reusable <see cref="Dictionary{TKey, TValue}"/> instances,
/// modeled on <see cref="System.Buffers.ArrayPool{T}"/>'s Rent/Return pattern.
/// </summary>
/// <remarks>
/// <para>
/// Backed by a <see cref="ConcurrentStack{T}"/>, a lock-free (CAS) linked stack:
/// <see cref="Rent"/> (<c>TryPop</c>) and <see cref="Return"/> (<c>Push</c>) are wait-free
/// and never take a monitor lock. Header dictionaries are rented and returned across
/// <c>await</c> boundaries (rent on the pump/handler thread, return on a dispatch
/// continuation thread), so there is no thread-local locality to exploit; a single
/// lock-free structure minimizes the unavoidable cross-thread synchronization cost.
/// </para>
/// <para>
/// A soft capacity cap (enforced via an atomic counter) bounds retention: returns
/// beyond the cap are dropped and left to the GC, so the pool never grows unbounded
/// under bursty or pathological load. The cap is just an upper bound on retention;
/// actual retention self-limits to the number of in-flight dictionaries.
/// </para>
/// <para>
/// <see cref="Return"/> calls <see cref="Dictionary{TKey, TValue}.Clear"/> by default,
/// which preserves the internal bucket/entry arrays (Capacity). For workloads where
/// successive usages need roughly the same number of entries (e.g. message headers,
/// which are uniform across a given endpoint), this means the preserved capacity
/// serves the next rent without any internal resize. <c>TrimExcess</c>
/// only fires when a returned dictionary's entry count exceeds
/// <c>maxRetainedCapacityPerItem</c>, a safety valve so one anomalously large usage
/// doesn't permanently inflate the pool's footprint. This is not the common path.
/// </para>
/// </remarks>
public class DictionaryPool<TKey, TValue> where TKey : notnull
{
    /// <summary>A shared, process-wide pool instance, analogous to <c>ArrayPool&lt;T&gt;.Shared</c>.</summary>
    public static DictionaryPool<TKey, TValue> Shared { get; } = new();

    readonly ConcurrentStack<Dictionary<TKey, TValue>> stack = [];
    readonly int maxPoolSize;
    readonly int maxRetainedCapacityPerItem;
    int count; // approximate size, maintained via Interlocked

    /// <summary>
    /// Approximate number of dictionaries currently retained in the pool.
    /// Thread-safe and safe to read at any time, but may lag behind the actual
    /// contents under concurrent access. Intended for diagnostics and testing.
    /// </summary>
    internal int Count => Interlocked.CompareExchange(ref count, 0, 0);

    /// <param name="maxPoolSize">
    /// Soft cap on the number of dictionaries retained. Returns beyond this limit
    /// are dropped rather than growing the pool unbounded. Defaults to a generous
    /// multiple of processor count so it's never the bottleneck under normal load.
    /// </param>
    /// <param name="maxRetainedCapacityPerItem">
    /// If a returned dictionary's entry count exceeds this, it is trimmed
    /// (<c>Clear</c> + <c>TrimExcess</c>) before being pooled, so one unusually large
    /// usage doesn't permanently inflate the pool's memory footprint. Defaults to
    /// 1024, well above typical header counts, so normal usage always takes the
    /// no-realloc path.
    /// </param>
    public DictionaryPool(int maxPoolSize = -1, int maxRetainedCapacityPerItem = 1024)
    {
        this.maxPoolSize = maxPoolSize > 0 ? maxPoolSize : Math.Max(Environment.ProcessorCount * 4, 64);
        this.maxRetainedCapacityPerItem = maxRetainedCapacityPerItem;
    }

    /// <summary>
    /// Rents a dictionary from the pool, or allocates a new one if the pool is
    /// currently empty. The returned dictionary is always empty.
    /// </summary>
    /// <param name="minimumCapacity">
    /// Optional capacity hint, mirroring <c>ArrayPool&lt;T&gt;.Rent(minimumLength)</c>.
    /// When provided, the returned dictionary is pre-sized so you can fill it
    /// without triggering internal resizes.
    /// </param>
    public Dictionary<TKey, TValue> Rent(int minimumCapacity = 0)
    {
        Dictionary<TKey, TValue> item;
        if (stack.TryPop(out var taken))
        {
            Interlocked.Decrement(ref count);
            item = taken;
        }
        else
        {
            item = [];
        }

        if (minimumCapacity > 0)
        {
            item.EnsureCapacity(minimumCapacity);
        }

        return item;
    }

    /// <summary>
    /// Returns a previously-rented dictionary to the pool for reuse.
    /// </summary>
    /// <param name="dictionary">The dictionary previously obtained from <see cref="Rent"/>.</param>
    /// <param name="clearDictionary">
    /// Whether to clear the dictionary's contents before pooling it. Defaults to true.
    /// Only pass false if you've already cleared it yourself; otherwise the next
    /// <see cref="Rent"/> caller sees stale data.
    /// </param>
    public void Return(Dictionary<TKey, TValue> dictionary, bool clearDictionary = true)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        bool tooLarge = dictionary.Count > maxRetainedCapacityPerItem;

        if (clearDictionary || tooLarge)
        {
            dictionary.Clear();
        }

        if (tooLarge)
        {
            // Release the oversized backing arrays so one outlier usage
            // doesn't permanently inflate the pool's memory footprint.
            dictionary.TrimExcess();
        }

        if (Interlocked.Increment(ref count) > maxPoolSize)
        {
            Interlocked.Decrement(ref count);
            return; // pool is full: drop it and let the GC reclaim it
        }

        stack.Push(dictionary);
    }
}