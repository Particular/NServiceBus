#nullable enable

namespace NServiceBus.Transport;

using System.Collections.Generic;
using NServiceBus.Utils;

/// <summary>
/// A pool of <c>Dictionary&lt;string, string&gt;</c> instances specialized for
/// message headers, with defaults tuned for typical header counts.
/// Inherits from <see cref="DictionaryPool{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// Use <see cref="Shared"/> for the process-wide instance. The retained-capacity
/// threshold defaults to 64, overriding the base class default of 1024, well
/// above typical header counts, so normal usage always takes the no-realloc
/// path on reuse.
/// </remarks>
public class HeaderPool : DictionaryPool<string, string>
{
    /// <summary>A shared, process-wide header pool instance.</summary>
    public static new HeaderPool Shared { get; } = new();

    /// <summary>
    /// An always-allocating header pool: renting always allocates a fresh dictionary
    /// and returning discards it. Use it where pooling is disabled, so rent and return
    /// sites stay branch-free.
    /// </summary>
    public static new HeaderPool AlwaysAllocate { get; } = new AlwaysAllocateHeaderPool();

    /// <param name="maxPoolSize">
    /// Soft cap on the number of dictionaries retained. Defaults to a generous
    /// multiple of processor count.
    /// </param>
    /// <param name="maxRetainedCapacityPerItem">
    /// If a returned dictionary's entry count exceeds this, it is trimmed before
    /// being pooled. Defaults to 64, overriding the base class default of 1024,
    /// well above typical header counts.
    /// </param>
    public HeaderPool(int maxPoolSize = -1, int maxRetainedCapacityPerItem = 64)
        : base(maxPoolSize, maxRetainedCapacityPerItem)
    {
    }

    sealed class AlwaysAllocateHeaderPool : HeaderPool
    {
        public override Dictionary<string, string> Rent(int minimumCapacity = 0) =>
            minimumCapacity > 0 ? new Dictionary<string, string>(capacity: minimumCapacity) : [];

        // Discarding without clearing: the caller may still legitimately read the
        // dictionary after a no-op return, and the next rent never sees this instance.
        public override void Return(Dictionary<string, string> dictionary, bool clearDictionary = true) { }
    }
}