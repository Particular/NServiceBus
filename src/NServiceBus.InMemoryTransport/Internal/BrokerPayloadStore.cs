namespace NServiceBus;

using System;
using System.Buffers;
using System.Collections.Generic;

public sealed class BrokerPayloadStore
{
    public static ArrayPool<byte> Pool => ArrayPool<byte>.Shared;

    public static BrokerEnvelope Borrow(
        string messageId,
        ReadOnlySpan<byte> payload,
        IReadOnlyDictionary<string, string> headers,
        string destination,
        bool isPublished,
        long sequenceNumber,
        DateTimeOffset? deliverAt = null,
        DateTimeOffset? discardAfter = null)
    {
        var buffer = Pool.Rent(payload.Length);
        payload.CopyTo(buffer);
        var body = new ReadOnlyMemory<byte>(buffer, 0, payload.Length);
        var headersCopy = new Dictionary<string, string>(headers);
        return new BrokerEnvelope(messageId, body, headersCopy, destination, isPublished, sequenceNumber, deliverAt, discardAfter)
        {
            Pool = Pool,
            Buffer = buffer
        };
    }
}
