#nullable enable

namespace NServiceBus.Utils;

using System.Collections.Generic;

/// <summary>
/// Extension methods for dictionary operations.
/// </summary>
public static class ReadOnlyDictionaryExtensions
{
    /// <summary>
    /// Copies all entries from <paramref name="source"/> into <paramref name="destination"/>,
    /// overwriting any existing keys. The destination dictionary is not cleared first.
    /// </summary>
    public static void CopyTo<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, Dictionary<TKey, TValue> destination) where TKey : notnull
    {
        foreach (var kvp in source)
        {
            destination[kvp.Key] = kvp.Value;
        }
    }
}