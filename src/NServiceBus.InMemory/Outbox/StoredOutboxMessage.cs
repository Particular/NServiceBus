#nullable enable

namespace NServiceBus.Persistence.InMemory;

using System;
using Outbox;

class StoredOutboxMessage(string messageId, TransportOperation[] transportOperations)
{
    public string MessageId { get; } = messageId;

    public bool Dispatched { get; private set; }

    public DateTime StoredAt { get; internal set; } = DateTime.UtcNow;

    public DateTime? DispatchedAt { get; private set; }

    public TransportOperation[] TransportOperations { get; private set; } = transportOperations;

    public void MarkAsDispatched(DateTime dispatchedAt)
    {
        Dispatched = true;
        DispatchedAt = dispatchedAt;
        TransportOperations = [];
    }
}
