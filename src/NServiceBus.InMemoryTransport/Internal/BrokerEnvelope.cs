namespace NServiceBus;

using System;
using System.Buffers;
using System.Collections.Generic;

public sealed record BrokerEnvelope(
    string MessageId,
    ReadOnlyMemory<byte> Body,
    IReadOnlyDictionary<string, string> Headers,
    string Destination,
    bool IsPublished,
    long SequenceNumber,
    DateTimeOffset? DeliverAt = null) : IDisposable
{
    public required ArrayPool<byte> Pool { get; init; }
    public required byte[] Buffer { get; init; }

    public void Dispose()
    {
        if (Buffer != null && Pool != null)
        {
            Pool.Return(Buffer, clearArray: true);
        }
    }
}
