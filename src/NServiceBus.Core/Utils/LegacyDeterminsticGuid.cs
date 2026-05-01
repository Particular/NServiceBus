#nullable enable

namespace NServiceBus;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Utils;

/// <summary>
/// This is a legacy implementation of <see cref="DeterministicGuid"/> that is used to generate a deterministic guid in places where
/// the new implementation would break existing assumptions.
///
/// This class should not be used anywhere else and should be removed once the legacy code that relies on it is removed as well.
/// We are moving away from using cryptographic hashes to generate deterministic guids because they are not necessary, and they are more expensive to compute than non-cryptographic hashes. The new implementation of <see cref="DeterministicGuid"/> uses a n
/// on-cryptographic hash algorithm to generate the guid, which is faster and still provides a good level of uniqueness for our use cases.
/// </summary>
static class LegacyDeterministicGuid
{
    public static Guid Create(string data1, string data2) => Create($"{data1}{data2}");

    [SkipLocalsInit]
    public static Guid Create(string data)
    {
        const int MaxStackLimit = 256;
        var encoding = Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(data.Length);

        byte[]? sharedBuffer = null;
        var stringBufferSpan = maxByteCount <= MaxStackLimit ?
            stackalloc byte[MaxStackLimit] :
            sharedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var numberOfBytesWritten = encoding.GetBytes(data, stringBufferSpan);
            Span<byte> hashBytes = stackalloc byte[16];

            _ = MD5.HashData(stringBufferSpan[..numberOfBytesWritten], hashBytes);

            // generate a guid from the hash:
            return new Guid(hashBytes);
        }
        finally
        {
            if (sharedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer, clearArray: true);
            }
        }
    }
}