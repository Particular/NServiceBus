#nullable enable

namespace NServiceBus.Persistence.InMemory;

using System;
using Outbox;

class StoredOutboxMessage(string messageId, TransportOperation[] transportOperations)
{
    public string MessageId { get; } = messageId;

    public bool Dispatched { get; private set; }

    public DateTime StoredAt { get; } = DateTime.UtcNow;

    public TransportOperation[] TransportOperations { get; private set; } = transportOperations;

    public void MarkAsDispatched()
    {
        Dispatched = true;
        TransportOperations = [];
    }
}
