#nullable enable

namespace NServiceBus.Transport;

using NServiceBus.Utils;

/// <summary>
/// A pool of <c>Dictionary&lt;string, string&gt;</c> instances specialized for
/// message headers, with defaults tuned for typical header counts.
/// Inherits from <see cref="DictionaryPool{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// Use <see cref="Shared"/> for the process-wide instance. The retained-capacity
/// threshold defaults to 64, well above typical header counts, so normal usage
/// always takes the no-realloc path on reuse.
/// </remarks>
public class HeaderPool : DictionaryPool<string, string>
{
    /// <summary>A shared, process-wide header pool instance.</summary>
    public static new HeaderPool Shared { get; } = new();

    /// <param name="maxPoolSize">
    /// Soft cap on the number of dictionaries retained. Defaults to a generous
    /// multiple of processor count.
    /// </param>
    /// <param name="maxRetainedCapacityPerItem">
    /// If a returned dictionary's entry count exceeds this, it is trimmed before
    /// being pooled. Defaults to 64, well above typical header counts.
    /// </param>
    public HeaderPool(int maxPoolSize = -1, int maxRetainedCapacityPerItem = 64)
        : base(maxPoolSize, maxRetainedCapacityPerItem)
    {
    }
}