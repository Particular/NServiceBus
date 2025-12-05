namespace NServiceBus;

using AcceptanceTesting;
using Persistence;

public abstract class AcceptanceTestingPersistenceBase : PersistenceDefinition
{
    protected AcceptanceTestingPersistenceBase(StorageType.SagasOptions? sagasOptions = null)
    {
        Supports<StorageType.Sagas, AcceptanceTestingSagaPersistence>(sagasOptions);
        Supports<StorageType.Subscriptions, AcceptanceTestingSubscriptionPersistence>();
        Supports<StorageType.Outbox, AcceptanceTestingOutboxPersistence>();
    }
}

public class AcceptanceTestingPersistence : AcceptanceTestingPersistenceBase, IPersistenceDefinitionFactory<AcceptanceTestingPersistence>
{
    AcceptanceTestingPersistence() : base(new StorageType.SagasOptions { SupportsFinders = true })
    {
    }

    static AcceptanceTestingPersistence IPersistenceDefinitionFactory<AcceptanceTestingPersistence>.Create() => new();
}

public class AcceptanceTestingPersistenceWithoutFinderSupport : AcceptanceTestingPersistenceBase, IPersistenceDefinitionFactory<AcceptanceTestingPersistenceWithoutFinderSupport>
{
    AcceptanceTestingPersistenceWithoutFinderSupport() : base(new StorageType.SagasOptions { SupportsFinders = false })
    {
    }

    static AcceptanceTestingPersistenceWithoutFinderSupport IPersistenceDefinitionFactory<AcceptanceTestingPersistenceWithoutFinderSupport>.Create() => new();
}