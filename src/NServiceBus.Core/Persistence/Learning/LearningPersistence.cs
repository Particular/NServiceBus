namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// Used to enable Development persistence.
    /// </summary>
    public class LearningPersistence : PersistenceDefinition
    {
        internal LearningPersistence()
        {
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<LearningSagaPersistence>());
        }
    }
}