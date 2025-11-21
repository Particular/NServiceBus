#nullable enable

namespace NServiceBus.Persistence;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    protected void Supports<TStorage, TFeature>(StorageType.Options? options = null)
        where TStorage : StorageType, new()
        where TFeature : Feature, new()
    {
        var storageType = new TStorage();
        if (storageToFeature.TryGetValue(storageType, out var feature))
        {
            throw new Exception($"Storage {storageType} is already supported by {feature.SupportedBy}");
        }

        if (options is not null && !storageType.Supports(options))
        {
            throw new Exception($"Storage {storageType} does not support the options {options}");
        }

        storageToFeature.Add(new StorageFeature<TFeature>(storageType, options));
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
    public bool HasSupportFor<T>() where T : StorageType, new() => HasSupportFor(new T());

    internal string Name => GetType().Name;
    internal string FullName => GetType().FullName ?? Name;

    internal bool HasSupportFor(StorageType storageType) => storageToFeature.Contains(storageType);

    internal void Apply(StorageType forStorageType, FeatureComponent.Settings toSettings)
        => storageToFeature[forStorageType].Apply(toSettings);

    internal void ApplyDefaults(SettingsHolder settings)
    {
        foreach (var @default in defaults)
        {
            @default(settings);
        }
    }

    internal IReadOnlyCollection<(StorageType Storage, StorageType.Options Options)> GetSupportedStorages(IReadOnlyCollection<StorageType> selectedStorages)
    {
        if (selectedStorages.Count > 0)
        {
            var storages = new List<(StorageType Storage, StorageType.Options Options)>();
            foreach (var selectedStorage in selectedStorages)
            {
                var storageFeature = storageToFeature.SingleOrDefault(s => s.StorageType.Equals(selectedStorage));
                if (storageFeature is not null)
                {
                    storages.Add((storageFeature.StorageType, storageFeature.Options));
                    continue;
                }

                storages.Add((selectedStorage, selectedStorage.Defaults));
            }
            return storages;
        }

        return [.. storageToFeature.Select(x => (x.StorageType, x.Options))];
    }

    readonly List<Action<SettingsHolder>> defaults = [];
    readonly StorageTypeToFeatureMap storageToFeature = [];

    class StorageTypeToFeatureMap : KeyedCollection<StorageType, IStorageFeature>
    {
        protected override StorageType GetKeyForItem(IStorageFeature item) => item.StorageType;
    }

    interface IStorageFeature
    {
        StorageType StorageType { get; }
        StorageType.Options Options { get; }

        string SupportedBy { get; }

        void Apply(FeatureComponent.Settings settings);
    }
    class StorageFeature<TFeature>(StorageType storageType, StorageType.Options? options) : IStorageFeature
        where TFeature : Feature, new()
    {
        public string SupportedBy { get; } = Feature.GetFeatureName<TFeature>();
        public StorageType StorageType { get; } = storageType;
        public StorageType.Options Options { get; } = options ?? storageType.Defaults;
        public void Apply(FeatureComponent.Settings settings) => settings.EnableFeature<TFeature>();
    }
}