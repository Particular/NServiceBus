namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using Persistence.InMemory;
using Unicast.Subscriptions;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

/// <summary>
/// Shared in-memory persistence runtime for development and testing scenarios.
/// </summary>
public class InMemoryStorage
{
    internal ConcurrentDictionary<Guid, SagaEntry> Sagas { get; } = new();

    internal ConcurrentDictionary<CorrelationId, Guid> SagaCorrelationIds { get; } = new();

    internal ConcurrentDictionary<string, StoredOutboxMessage> OutboxMessages { get; } = new(StringComparer.Ordinal);

    internal ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>> Subscriptions { get; } = new();
}