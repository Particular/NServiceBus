namespace NServiceBus.Core.Analyzer.Utility;

using System;

/// <summary>
/// 64-bit FNV-1a over chars, https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
/// This is a fast-enough, non-cryptographic hash function. Unfortunately, we can't use the built-in one because it's not available in netstandard2.0
/// </summary>
public static class NonCryptographicHash
{
    const ulong offsetBasis = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;

    public static ulong GetHash(params ReadOnlySpan<string> parts)
    {
        ulong hash = offsetBasis;

        for (int index = 0; index < parts.Length; index++)
        {
            string part = parts[index];
            ReadOnlySpan<char> span = part.AsSpan();
            int length = span.Length;
            int i = 0;
            // Process 4 chars at a time
            for (; i + 3 < length; i += 4)
            {
                hash ^= span[i];
                hash *= prime;

                hash ^= span[i + 1];
                hash *= prime;

                hash ^= span[i + 2];
                hash *= prime;

                hash ^= span[i + 3];
                hash *= prime;
            }

            // Handle remainder (0–3 chars)
            for (; i < length; i++)
            {
                hash ^= span[i];
                hash *= prime;
            }
        }

        return hash;
    }
}