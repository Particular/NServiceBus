namespace NServiceBus;

using Features;
using Persistence;

/// <summary>
/// Used to enable Learning persistence.
/// </summary>
public class LearningPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<LearningPersistence>
{
    LearningPersistence() => Supports<StorageType.Sagas, LearningSagaPersistence>();
    public static LearningPersistence Create() => new();
}