#nullable enable

namespace NServiceBus;

using System;
using Persistence;

/// <summary>
/// Enables users to select persistence by calling .UsePersistence().
/// </summary>
public static class PersistenceConfig
{
    /// <summary>
    /// Configures the given persistence to be used.
    /// </summary>
    /// <typeparam name="T">The persistence definition eg <see cref="LearningPersistence" />, NHibernate etc.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    public static PersistenceExtensions<T> UsePersistence<T>(this EndpointConfiguration config)
        where T : PersistenceDefinition, IPersistenceDefinitionFactory<T>
    {
        ArgumentNullException.ThrowIfNull(config);
        return new PersistenceExtensions<T>(config.Settings);
    }

    /// <summary>
    /// Configures the given persistence to be used for a specific storage type.
    /// </summary>
    /// <typeparam name="T">The persistence definition eg <see cref="LearningPersistence" />, NHibernate etc.</typeparam>
    /// <typeparam name="S">The <see cref="StorageType" />storage type.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    public static PersistenceExtensions<T, S> UsePersistence<T, S>(this EndpointConfiguration config)
        where T : PersistenceDefinition, IPersistenceDefinitionFactory<T>
        where S : StorageType
    {
        ArgumentNullException.ThrowIfNull(config);
        return new PersistenceExtensions<T, S>(config.Settings);
    }

    /// <summary>
    /// Configures the given persistence to be used.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="definitionType">The persistence definition eg <see cref="LearningPersistence" />, NHibernate etc.</param>
    // TODO obsolete?
    public static PersistenceExtensions UsePersistence(this EndpointConfiguration config, Type definitionType)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(definitionType);
        return new PersistenceExtensions(definitionType, config.Settings, null);
    }
}