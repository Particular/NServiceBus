#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Configuration.AdvancedExtensibility;
using Persistence;
using Settings;

/// <summary>
/// This class provides implementers of persisters with an extension mechanism for custom settings for specific storage
/// type via extension methods.
/// </summary>
/// <typeparam name="T">The persister definition eg <see cref="LearningPersistence" />, etc.</typeparam>
/// <typeparam name="S">The <see cref="StorageType" />storage type.</typeparam>
public class PersistenceExtensions<T, S> : PersistenceExtensions<T>
    where T : PersistenceDefinition, IPersistenceDefinitionFactory<T>
    where S : StorageType
{
    /// <summary>
    /// Initializes a new instance of <see cref="PersistenceExtensions" />.
    /// </summary>
    public PersistenceExtensions(SettingsHolder settings) : base(settings, StorageType.Get<S>())
    {
    }
}

/// <summary>
/// This class provides implementers of persisters with an extension mechanism for custom settings via extension
/// methods.
/// </summary>
/// <typeparam name="T">The persister definition eg <see cref="LearningPersistence" />, etc.</typeparam>
public partial class PersistenceExtensions<T> : ExposeSettings
    where T : PersistenceDefinition, IPersistenceDefinitionFactory<T>
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public PersistenceExtensions(SettingsHolder settings) : this(settings, default(StorageType))
    {
    }

    /// <summary>
    /// Constructor for a specific <see cref="StorageType" />.
    /// </summary>
    protected PersistenceExtensions(SettingsHolder settings, StorageType? storageType = null) : base(settings)
    {
        var registry = settings
            .GetOrCreate<PersistenceRegistry>()
            .Enable<T>(settings);
        if (storageType is not null)
        {
            registry.WithStorage(storageType);
        }
    }
}