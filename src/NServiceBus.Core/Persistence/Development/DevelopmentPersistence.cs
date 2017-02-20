namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// Used to enable Development persistence.
    /// </summary>
    public class DevelopmentPersistence : PersistenceDefinition
    {
        internal DevelopmentPersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<DevelopmentSagaPersistence>());
        }
    }
}