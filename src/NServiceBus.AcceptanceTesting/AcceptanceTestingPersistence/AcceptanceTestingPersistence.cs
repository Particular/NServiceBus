namespace NServiceBus
{
    using Features;
    using AcceptanceTesting;
    using Persistence;

    public class AcceptanceTestingPersistence : PersistenceDefinition
    {
        internal AcceptanceTestingPersistence()
        {
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<AcceptanceTestingSagaPersistence>();
                s.EnableFeatureByDefault<AcceptanceTestingTransactionalStorageFeature>();
            });

            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<AcceptanceTestingTimeoutPersistence>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<AcceptanceTestingSubscriptionPersistence>());
            Supports<StorageType.Outbox>(s =>
            {
                s.EnableFeatureByDefault<AcceptanceTestingOutboxPersistence>();
                s.EnableFeatureByDefault<AcceptanceTestingTransactionalStorageFeature>();
            });
        }
    }
}