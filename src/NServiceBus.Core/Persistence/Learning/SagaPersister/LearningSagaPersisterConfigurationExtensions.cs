﻿namespace NServiceBus;

using System;
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
}