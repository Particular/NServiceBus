namespace NServiceBus.Persistence;

using System;
using System.Collections.Generic;
using Features;
using Settings;

/// <summary>
/// Base class for persistence definitions.
/// </summary>
public abstract class PersistenceDefinition
{
    /// <summary>
    /// Used by the storage definitions to declare what they support.
    /// </summary>
    protected void Supports<TStorage, TFeature>()
        where TStorage : StorageType
        where TFeature : Feature
    {
        var storageType = StorageType.Get<TStorage>();
        if (storageToFeatureMap.TryGetValue(storageType, out Type supportedStorageType))
        {
            throw new Exception($"Storage {typeof(TStorage)} is already supported by {supportedStorageType}");
        }

        storageToFeatureMap[storageType] = typeof(TFeature);
    }

    /// <summary>
    /// Used by the storage definitions to declare what they support.
    /// </summary>
#pragma warning disable CA1822
    protected void Supports<T>(Action<SettingsHolder> action) where T : StorageType
#pragma warning restore CA1822
    {
        // TODO obsolete
    }

    /// <summary>
    /// Used by the storage definitions to declare what they support.
    /// </summary>
    protected void Defaults(Action<SettingsHolder> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        defaults.Add(action);
    }

    /// <summary>
    /// True if supplied storage is supported.
    /// </summary>
    public bool HasSupportFor<T>() where T : StorageType => storageToFeatureMap.ContainsKey(StorageType.Get<T>());

    internal bool HasSupportFor(StorageType storageType) => storageToFeatureMap.ContainsKey(storageType);

    /// <summary>
    /// True if supplied storage is supported.
    /// </summary>
#pragma warning disable CA1822
    public bool HasSupportFor(Type storageType)
#pragma warning restore CA1822
    {
        // TODO
        ArgumentNullException.ThrowIfNull(storageType);
        return false;
    }

    internal void ApplyActionForStorage(StorageType storageType, SettingsHolder settings)
    {
        var featureSupportingStorage = storageToFeatureMap[storageType];
        _ = settings.EnableFeatureByDefault(featureSupportingStorage);
    }

    internal void ApplyDefaults(SettingsHolder settings)
    {
        foreach (var @default in defaults)
        {
            @default(settings);
        }
    }

    internal IReadOnlyCollection<StorageType> GetSupportedStorages(IReadOnlyCollection<StorageType> selectedStorages) =>
        selectedStorages.Count > 0 ? selectedStorages : [.. storageToFeatureMap.Keys];

    readonly List<Action<SettingsHolder>> defaults = [];
    readonly Dictionary<StorageType, Type> storageToFeatureMap = [];
}