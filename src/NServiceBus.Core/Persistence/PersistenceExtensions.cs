#nullable enable

namespace NServiceBus;

using Configuration.AdvancedExtensibility;
using Persistence;
using Settings;

/// <summary>
/// This class provides implementers of persisters with an extension mechanism for custom settings for specific storage
/// type via extension methods.
/// </summary>
/// <typeparam name="T">The persister definition eg <see cref="LearningPersistence" />, etc.</typeparam>
/// <typeparam name="S">The <see cref="StorageType" />storage type.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="PersistenceExtensions" />.
/// </remarks>
public class PersistenceExtensions<T, S>(SettingsHolder settings) : PersistenceExtensions<T>(settings, StorageType.Get<S>())
    where T : PersistenceDefinition, IPersistenceDefinitionFactory<T>
    where S : StorageType;

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

    internal PersistenceExtensions(SettingsHolder settings, StorageType? storageType = null) : base(settings)
    {
        var registry = settings.GetOrCreate<PersistenceComponent.Settings>();
        registry.Enable<T>(storageType);
    }
}