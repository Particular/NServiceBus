namespace NServiceBus;

using System;
using System.Collections.Generic;

public sealed record BrokerEnvelope(
    string MessageId,
    ReadOnlyMemory<byte> Body,
    IReadOnlyDictionary<string, string> Headers,
    string Destination,
    bool IsPublished,
    long SequenceNumber,
    DateTimeOffset? DeliverAt = null)
{
    public static BrokerEnvelope Create(
        string messageId,
        byte[] body,
        IReadOnlyDictionary<string, string> headers,
        string destination,
        bool isPublished,
        long sequenceNumber,
        DateTimeOffset? deliverAt = null)
    {
        var headersCopy = new Dictionary<string, string>(headers);
        return new BrokerEnvelope(messageId, body, headersCopy, destination, isPublished, sequenceNumber, deliverAt);
    }
}
