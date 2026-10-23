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
        // Enumerating through the interface boxes Dictionary's struct enumerator, which would add an
        // allocation to every header copy. Message headers are almost always a Dictionary.
        if (source is Dictionary<TKey, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                destination[kvp.Key] = kvp.Value;
            }

            return;
        }

        foreach (var kvp in source)
        {
            destination[kvp.Key] = kvp.Value;
        }
    }
}