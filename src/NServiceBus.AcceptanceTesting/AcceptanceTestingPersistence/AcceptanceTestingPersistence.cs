namespace NServiceBus;

using AcceptanceTesting;
using Persistence;

public class AcceptanceTestingPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<AcceptanceTestingPersistence>
{
    AcceptanceTestingPersistence()
    {
        Supports<StorageType.Sagas, AcceptanceTestingSagaPersistence>();
        Supports<StorageType.Subscriptions, AcceptanceTestingSubscriptionPersistence>();
        Supports<StorageType.Outbox, AcceptanceTestingOutboxPersistence>();
    }

    static AcceptanceTestingPersistence IPersistenceDefinitionFactory<AcceptanceTestingPersistence>.Create() => new();
}