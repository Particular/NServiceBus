namespace NServiceBus;

using AcceptanceTesting;
using Persistence;
using Settings;

public class AcceptanceTestingPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<AcceptanceTestingPersistence>
{
    AcceptanceTestingPersistence(SettingsHolder settingsHolder)
    {
        settingsHolder.Set();
        Supports<StorageType.Sagas, AcceptanceTestingSagaPersistence>();
        Supports<StorageType.Subscriptions, AcceptanceTestingSubscriptionPersistence>();
        Supports<StorageType.Outbox, AcceptanceTestingOutboxPersistence>();
    }

    public static AcceptanceTestingPersistence Create(SettingsHolder settingsHolder) => new(settingsHolder);
}