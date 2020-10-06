namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// Used to enable InMemory persistence.
    /// </summary>
    public class InMemoryPersistence : PersistenceDefinition
    {
        internal InMemoryPersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<InMemorySagaPersistence>());
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<InMemoryTimeoutPersistence>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<InMemoryOutboxPersistence>());
        }
    }
}