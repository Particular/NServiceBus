namespace NServiceBus.Core.Analyzer.Utility;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
            ref char first = ref MemoryMarshal.GetReference(span);
            int length = span.Length;
            int i = 0;
            // Process 4 chars at a time
            for (; i + 3 < length; i += 4)
            {
                char c0 = Unsafe.Add(ref first, i);
                hash ^= c0;
                hash *= prime;

                char c1 = Unsafe.Add(ref first, i + 1);
                hash ^= c1;
                hash *= prime;

                char c2 = Unsafe.Add(ref first, i + 2);
                hash ^= c2;
                hash *= prime;

                char c3 = Unsafe.Add(ref first, i + 3);
                hash ^= c3;
                hash *= prime;
            }

            // Handle remainder (0–3 chars)
            for (; i < length; i++)
            {
                char c = Unsafe.Add(ref first, i);
                hash ^= c;
                hash *= prime;
            }
        }

        return hash;
    }
}