#nullable enable

namespace NServiceBus;

using Features;
using Persistence;
using Sagas;

/// <summary>
/// Used to enable Learning persistence.
/// </summary>
public class LearningPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<LearningPersistence>, ISupportTimeoutManagement
{
    LearningPersistence() => Supports<StorageType.Sagas, LearningSagaPersistence>();

    /// <summary>
    /// Creates the learning persistence definition.
    /// </summary>
    static LearningPersistence IPersistenceDefinitionFactory<LearningPersistence>.Create() => new();
}