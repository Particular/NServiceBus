namespace NServiceBus;

using AcceptanceTesting;
using Persistence;

public class AcceptanceTestingPersistence : PersistenceDefinition
{
    internal AcceptanceTestingPersistence()
    {
        Supports<StorageType.Sagas, AcceptanceTestingSagaPersistence>();
        Supports<StorageType.Subscriptions, AcceptanceTestingSubscriptionPersistence>();
        Supports<StorageType.Outbox, AcceptanceTestingOutboxPersistence>();
    }
}