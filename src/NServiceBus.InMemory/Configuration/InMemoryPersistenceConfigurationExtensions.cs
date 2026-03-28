#nullable enable

namespace NServiceBus;

using System;
using System.Text.Json;
using Configuration.AdvancedExtensibility;
using Persistence.InMemory;

/// <summary>
/// Extension methods for configuring in-memory persistence.
/// </summary>
public static class InMemoryPersistenceConfigurationExtensions
{
    /// <summary>
    /// Configures the endpoint to use in-memory persistence.
    /// </summary>
    public static PersistenceExtensions<InMemoryPersistence> UseInMemoryPersistence(
        this EndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.UsePersistence<InMemoryPersistence>();
    }

    /// <summary>
    /// Configures the <see cref="JsonSerializerOptions"/> used for serializing saga data.
    /// </summary>
    /// <param name="persistenceExtensions">The persistence extensions to extend.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    public static void SerializerOptions(this PersistenceExtensions<InMemoryPersistence> persistenceExtensions, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(persistenceExtensions);
        ArgumentNullException.ThrowIfNull(options);

        persistenceExtensions.GetSettings().Set(InMemorySagaPersistence.SerializerOptionsKey, options);
    }

    /// <summary>
    /// Configures the <see cref="InMemoryStorage"/> runtime used by the in-memory persistence.
    /// </summary>
    public static void Storage(this PersistenceExtensions<InMemoryPersistence> persistenceExtensions, InMemoryStorage storage)
    {
        ArgumentNullException.ThrowIfNull(persistenceExtensions);
        ArgumentNullException.ThrowIfNull(storage);

        persistenceExtensions.GetSettings().Set(InMemoryStorageRuntime.StorageKey, storage);
    }
}