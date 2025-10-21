#nullable enable

namespace NServiceBus.Persistence;

using System;
using System.Collections.Generic;
using Features;
using Settings;

/// <summary>
/// Base class for persistence definitions.
/// </summary>
public abstract partial class PersistenceDefinition
{
    /// <summary>
    /// Used by the storage definitions to declare what they support.
    /// </summary>
    protected void Supports<TStorage, TFeature>()
        where TStorage : StorageType
        where TFeature : Feature
    {
        var storageType = StorageType.Get<TStorage>();
        if (storageToFeatureMap.TryGetValue(storageType, out var supportedStorageType))
        {
            throw new Exception($"Storage {typeof(TStorage)} is already supported by {supportedStorageType}");
        }

        storageToFeatureMap[storageType] = typeof(TFeature);
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
    public bool HasSupportFor<T>() where T : StorageType => HasSupportFor(StorageType.Get<T>());

    internal string Name => GetType().Name;
    internal string FullName => GetType().FullName ?? Name;

    internal bool HasSupportFor(StorageType storageType) => storageToFeatureMap.ContainsKey(storageType);

    internal Type GetFeatureForStorage(StorageType storageType) => storageToFeatureMap[storageType];

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