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

        // UUID version 8
        hash[6] = (byte)((hash[6] & 0x0F) | 0x80);

        // RFC 4122 / RFC 9562 variant
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        return new Guid(hash, bigEndian: true);
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
        var encoding = Encoding.UTF8;

        var totalByteCount = 0;

        Span<int> counts = values.Length <= 64
            ? stackalloc int[values.Length]
            : new int[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var count = encoding.GetByteCount(values[i]);
            counts[i] = count;

            totalByteCount = checked(totalByteCount + LengthPrefixSize + count);
        }

        byte[]? rented = null;

        Span<byte> buffer = totalByteCount <= MaxStackLimit
            ? stackalloc byte[MaxStackLimit]
            : rented = ArrayPool<byte>.Shared.Rent(totalByteCount);

        try
        {
            var written = 0;

            for (var i = 0; i < values.Length; i++)
            {
                var count = counts[i];

                BinaryPrimitives.WriteInt32LittleEndian(
                    buffer.Slice(written, LengthPrefixSize),
                    count);

                written += LengthPrefixSize;

                written += encoding.GetBytes(
                    values[i],
                    buffer[written..]);
            }

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
}