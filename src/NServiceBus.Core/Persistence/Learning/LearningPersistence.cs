namespace NServiceBus;

using Features;
using Persistence;

/// <summary>
/// Used to enable Learning persistence.
/// </summary>
public class LearningPersistence : PersistenceDefinition
{
    internal LearningPersistence()
    {
        Defaults(s => s.EnableFeatureByDefault<LearningSynchronizedStorage>());

        Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<LearningSagaPersistence>());
    }
}