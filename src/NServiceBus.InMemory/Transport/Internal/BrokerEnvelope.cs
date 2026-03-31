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
    DateTimeOffset? DeliverAt = null,
    DateTimeOffset? DiscardAfter = null,
    int DeliveryAttempt = 1) : IDisposable
{
    public required ArrayPool<byte> Pool { get; init; }
    public required byte[] Buffer { get; init; }
    internal InlineEnvelopeState? InlineState { get; init; }

    public BrokerEnvelope WithDeliveryAttempt(int attempt) =>
        CloneWith(DeliveryAttempt: attempt);

    public BrokerEnvelope WithDeliverAt(DateTimeOffset deliverAt) =>
        CloneWith(DeliverAt: deliverAt);

    BrokerEnvelope CloneWith(DateTimeOffset? DeliverAt = null, int? DeliveryAttempt = null) =>
        this with
        {
            Headers = new Dictionary<string, string>(Headers),
            DeliverAt = DeliverAt ?? this.DeliverAt,
            DeliveryAttempt = DeliveryAttempt ?? this.DeliveryAttempt
        };

    public void Dispose() => Pool.Return(Buffer, clearArray: true);
}
