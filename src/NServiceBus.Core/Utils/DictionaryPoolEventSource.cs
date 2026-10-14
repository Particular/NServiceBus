#nullable enable

namespace NServiceBus;

using System.Diagnostics.Tracing;
using NServiceBus.Utils;

/// <summary>
/// ETW/EventSource diagnostics for <see cref="DictionaryPool{TKey, TValue}"/>,
/// modeled on <c>System.Buffers.ArrayPoolEventSource</c>. The event methods are
/// effectively no-ops when no listener is attached, so rent/return paths keep
/// their wait-free performance budget.
/// </summary>
[EventSource(Name = "NServiceBus.DictionaryPool")]
sealed class DictionaryPoolEventSource : EventSource
{
    /// <summary>The singleton instance that pools report to.</summary>
    internal static readonly DictionaryPoolEventSource Log = new();

    /// <summary>Reasons a returned dictionary was not retained by the pool.</summary>
    internal enum DictionaryDroppedReason
    {
        /// <summary>The pool was at its soft capacity cap when the dictionary was returned.</summary>
        PoolFull
    }

    /// <summary>
    /// Fired once for every successful <see cref="DictionaryPool{TKey, TValue}.Rent"/> call,
    /// whether the dictionary came from the pool or was newly allocated. Verbose because
    /// rents occur on the hot path (typically more than 1000/sec on busy endpoints).
    /// </summary>
    [Event(1, Level = EventLevel.Verbose)]
    internal void DictionaryRented(int poolId, int minimumCapacity)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(1, poolId, minimumCapacity);
        }
    }

    /// <summary>
    /// Fired when <see cref="DictionaryPool{TKey, TValue}.Rent"/> allocated a new dictionary
    /// because the pool was empty. In steady state (rents balanced by returns) this should
    /// be rare; a high ratio of allocations to rents indicates the pool is too small for
    /// the workload.
    /// </summary>
    [Event(2, Level = EventLevel.Informational)]
    internal void DictionaryAllocated(int poolId, int minimumCapacity)
    {
        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(2, poolId, minimumCapacity);
        }
    }

    /// <summary>
    /// Fired once for every <see cref="DictionaryPool{TKey, TValue}.Return"/> call that put
    /// the dictionary back into the pool. Verbose for the same reason as
    /// <see cref="DictionaryRented"/>.
    /// </summary>
    [Event(3, Level = EventLevel.Verbose)]
    internal void DictionaryReturned(int poolId, int dictionaryCount)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(3, poolId, dictionaryCount);
        }
    }

    /// <summary>
    /// Fired when a returned dictionary was trimmed (<c>Clear</c> + <c>TrimExcess</c>) before
    /// pooling because its entry count exceeded <c>maxRetainedCapacityPerItem</c>.
    /// </summary>
    [Event(4, Level = EventLevel.Informational)]
    internal void DictionaryTrimmed(int poolId, int dictionaryCount, int maxRetainedCapacity)
    {
        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(4, poolId, dictionaryCount, maxRetainedCapacity);
        }
    }

    /// <summary>
    /// Fired when a returned dictionary was dropped instead of retained because the pool
    /// was full. The dropped dictionary is left to the garbage collector, so this is
    /// normal operation, not a leak.
    /// </summary>
    [Event(5, Level = EventLevel.Informational)]
    internal void DictionaryDropped(int poolId, int dictionaryCount, DictionaryDroppedReason reason)
    {
        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(5, poolId, dictionaryCount, (int)reason);
        }
    }

    /// <summary>
    /// Fired once when a <see cref="DictionaryPool{TKey, TValue}"/> is created, carrying the
    /// closed generic key and value types. Correlate other events to <paramref name="poolId"/>
    /// to know which pool type they belong to.
    /// </summary>
    [Event(6, Level = EventLevel.Informational)]
    internal void DictionaryPoolCreated(int poolId, string keyType, string valueType)
    {
        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(6, poolId, keyType, valueType);
        }
    }
}
