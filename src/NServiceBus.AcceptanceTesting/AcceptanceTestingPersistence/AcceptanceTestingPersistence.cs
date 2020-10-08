namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using Features;
    using Outbox;
    using Persistence;
    using SagaPersister;
    using TimeoutPersister;

    public class AcceptanceTestingPersistence : PersistenceDefinition
    {
        internal AcceptanceTestingPersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<AcceptanceTestingSagaPersistence>());
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<AcceptanceTestingTimeoutPersistence>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<AcceptanceTestingSubscriptionPersistence>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<AcceptanceTestingOutboxPersistence>());

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