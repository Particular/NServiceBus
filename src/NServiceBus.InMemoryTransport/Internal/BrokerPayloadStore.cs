namespace NServiceBus;

using System;
using System.Buffers;

public sealed class BrokerPayloadStore
{
    readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

    public byte[] Rent(int minimumLength)
    {
        return pool.Rent(minimumLength);
    }

    public void Return(byte[] array)
    {
        pool.Return(array);
    }

    public byte[] Copy(ReadOnlySpan<byte> payload)
    {
        var buffer = pool.Rent(payload.Length);
        payload.CopyTo(buffer);
        return buffer;
    }
}
