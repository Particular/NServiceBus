#nullable enable

namespace NServiceBus.Utils;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Creates deterministic version 8 GUIDs from binary or textual input.
/// </summary>
/// <remarks>
/// The same input always produces the same GUID.
///
/// Use this type when a stable identifier must be derived from existing data,
/// such as host identifiers, partition identifiers, or deterministic fallback IDs.
///
/// For generated time-ordered GUIDs optimized for insertion order or chronological
/// sorting, use <see cref="Guid.CreateVersion7()" /> instead.
/// </remarks>
public static class DeterministicGuid
{
    const int MaxStackLimit = 256;
    const int LengthPrefixSize = sizeof(int);

    // XxHash128(seed: 0) over empty input, formatted as UUID v8 / RFC 9562 variant.
    // Precomputed so Create() with no values avoids allocating a hash state on the hot path.
    static readonly Guid EmptyInputGuid = new("99aa06d3-0147-88d8-a001-c324468d497f");

    /// <summary>
    /// Creates a deterministic version 8 GUID from the specified string.
    /// </summary>
    /// <param name="value">The string value used to derive the GUID.</param>
    /// <returns>A deterministic version 8 GUID derived from <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <c>null</c>.
    /// </exception>
    public static Guid Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Create(value.AsSpan());
    }

    /// <summary>
    /// Creates a deterministic version 8 GUID from the specified UTF-16 character data.
    /// </summary>
    /// <param name="value">
    /// The character data used to derive the GUID. The value is encoded as UTF-8 before hashing.
    /// </param>
    /// <returns>A deterministic version 8 GUID derived from <paramref name="value" />.</returns>
    public static Guid Create(ReadOnlySpan<char> value)
    {
        var encoding = Encoding.UTF8;
        var byteCount = encoding.GetByteCount(value);

        byte[]? rented = null;

        Span<byte> buffer = byteCount <= MaxStackLimit
            ? stackalloc byte[MaxStackLimit]
            : rented = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            var written = encoding.GetBytes(value, buffer);
            return Create(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }
    }

    /// <summary>
    /// Creates a deterministic version 8 GUID from the specified binary data.
    /// </summary>
    /// <param name="value">The binary data used to derive the GUID.</param>
    /// <returns>A deterministic version 8 GUID derived from <paramref name="value" />.</returns>
    /// <remarks>
    /// The input is hashed directly. No text encoding or framing is applied.
    /// </remarks>
    public static Guid Create(ReadOnlySpan<byte> value)
    {
        Span<byte> hash = stackalloc byte[16];

        _ = XxHash128.Hash(value, hash);

        return FormatGuid(hash);
    }

    /// <summary>
    /// Creates a deterministic version 8 GUID from multiple string values.
    /// </summary>
    /// <param name="values">The string values used to derive the GUID.</param>
    /// <returns>A deterministic version 8 GUID derived from <paramref name="values" />.</returns>
    /// <remarks>
    /// Each value is UTF-8 encoded and length-prefixed before hashing. This avoids
    /// ambiguity between different value sequences that would otherwise produce the
    /// same concatenated text, such as <c>("ab", "c")</c> and <c>("a", "bc")</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any value in <paramref name="values" /> is <c>null</c>.
    /// </exception>
    [SkipLocalsInit]
    public static Guid Create(params ReadOnlySpan<string> values)
    {
        if (values.Length == 0)
        {
            return EmptyInputGuid;
        }

        var encoding = Encoding.UTF8;

        // Compute total buffer size using O(1) GetMaxByteCount instead of O(n) GetByteCount.
        // GetMaxByteCount over-estimates (up to 3× for ASCII) but avoids scanning each string
        // and enables a single ArrayPool rent instead of per-value rent/return cycles.
        var totalSize = 0;
        for (var i = 0; i < values.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(values[i]);
            totalSize += LengthPrefixSize + encoding.GetMaxByteCount(values[i].Length);
        }

        byte[]? rented = null;
        Span<byte> buffer = totalSize <= MaxStackLimit
            ? stackalloc byte[MaxStackLimit]
            : rented = ArrayPool<byte>.Shared.Rent(totalSize);

        try
        {
            var hash = new XxHash128();
            var offset = 0;

            for (var i = 0; i < values.Length; i++)
            {
                // Encode value after the length-prefix slot, then fill the prefix with actual count.
                var written = encoding.GetBytes(values[i], buffer[(offset + LengthPrefixSize)..]);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset, LengthPrefixSize), written);
                hash.Append(buffer.Slice(offset, LengthPrefixSize + written));
                offset += LengthPrefixSize + written;
            }

            Span<byte> hashBytes = stackalloc byte[16];
            _ = hash.GetCurrentHash(hashBytes);
            return FormatGuid(hashBytes);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Guid FormatGuid(Span<byte> hash)
    {
        // UUID version 8
        hash[6] = (byte)((hash[6] & 0x0F) | 0x80);

        // RFC 4122 / RFC 9562 variant
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        return new Guid(hash, bigEndian: true);
    }
}