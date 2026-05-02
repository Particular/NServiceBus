#nullable enable

namespace NServiceBus;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Particular.Obsoletes;
using Utils;

/// <summary>
/// This is a legacy implementation of <see cref="DeterministicGuid"/> that uses MD5 to generate deterministic GUIDs
/// for backward compatibility with endpoints that rely on the old host ID algorithm.
/// </summary>
/// <remarks>
/// In version 11, the new <see cref="DeterministicGuid" /> (XxHash128-based) becomes the default.
/// Endpoints that need to preserve the old MD5-based host IDs can opt in by setting the AppContext switch
/// <c>NServiceBus.Core.Hosting.UseV2DeterministicGuid</c> to <c>false</c>.
/// This class will be removed in version 12.
/// </remarks>
[PreObsolete("https://github.com/Particular/NServiceBus/issues/7734",
    Note = "In v11, DeterministicGuid (XxHash128) becomes the default. This class remains available for endpoints that need to preserve MD5-based host IDs by setting the AppContext switch NServiceBus.Core.Hosting.UseV2DeterministicGuid to false. Will be removed in v12.",
    ReplacementTypeOrMember = "DeterministicGuid")]
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