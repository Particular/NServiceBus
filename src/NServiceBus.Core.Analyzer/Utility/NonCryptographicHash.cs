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

    public static ulong GetHash(params string[] parts)
    {
        ulong hash = offsetBasis;

        foreach (var part in parts)
        {
            foreach (var ch in part.AsSpan())
            {
                hash ^= ch;
                hash *= prime;
            }
        }

        return hash;
    }
}