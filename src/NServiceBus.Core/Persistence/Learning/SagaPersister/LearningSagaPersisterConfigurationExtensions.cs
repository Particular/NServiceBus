#nullable enable

namespace NServiceBus;

using System;
using System.Text.Json;
using Features;

/// <summary>
/// Configuration options for the learning saga persister.
/// </summary>
public static class LearningSagaPersisterConfigurationExtensions
{
    /// <summary>
    /// Configures the location where sagas are stored.
    /// </summary>
    /// <param name="persistenceExtensions">The persistence extensions to extend.</param>
    /// <param name="path">The storage path.</param>
    public static void SagaStorageDirectory(this PersistenceExtensions<LearningPersistence> persistenceExtensions, string path)
    {
        ArgumentNullException.ThrowIfNull(persistenceExtensions);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        persistenceExtensions.Settings.Set(LearningSagaPersistence.StorageLocationKey, path);
    }

    /// <summary>
    /// Configures the <see cref="JsonSerializerOptions" /> to use for serializing saga data.
    /// </summary>
    /// <param name="persistenceExtensions">The persistence extensions to extend.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions" /> to use.</param>
    public static void SagaSerializerOptions(this PersistenceExtensions<LearningPersistence> persistenceExtensions, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(persistenceExtensions);
        ArgumentNullException.ThrowIfNull(options);

        persistenceExtensions.Settings.Set(LearningSagaPersistence.SerializerOptionsKey, options);
    }
}