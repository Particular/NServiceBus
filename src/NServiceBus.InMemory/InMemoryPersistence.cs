#nullable enable

namespace NServiceBus;

using Persistence;

/// <summary>
/// In-memory persistence implementation for development and testing scenarios.
/// </summary>
public class InMemoryPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<InMemoryPersistence>
{
    internal InMemoryPersistence()
    {
        Supports<StorageType.Sagas, Persistence.InMemory.InMemorySagaPersistence>();
        Supports<StorageType.Subscriptions, Persistence.InMemory.InMemorySubscriptionPersistence>();
        Supports<StorageType.Outbox, Persistence.InMemory.InMemoryOutboxPersistence>();
    }

    /// <summary>
    /// Creates the in-memory persistence definition.
    /// </summary>
    static InMemoryPersistence IPersistenceDefinitionFactory<InMemoryPersistence>.Create() => new();
}
