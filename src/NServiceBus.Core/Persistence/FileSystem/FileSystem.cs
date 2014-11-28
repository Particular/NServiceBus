namespace NServiceBus.Persistence.FileSystem
{
    using NServiceBus.Features;

    class FileSystemPersistence : PersistenceDefinition
    {
        internal FileSystemPersistence()
        {
            // TODO enable features

//            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<FileSystemSagaPersistence>());
//            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<FileSystemTimeoutPersistence>());
//            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<FileSystemSubscriptionPersistence>());
//            Supports(Storage.Outbox, s => s.EnableFeatureByDefault<FileSystemOutboxPersistence>());
//            Supports(Storage.GatewayDeduplication, s => s.EnableFeatureByDefault<FileSystemGatewayPersistence>());
        }
    }
}
