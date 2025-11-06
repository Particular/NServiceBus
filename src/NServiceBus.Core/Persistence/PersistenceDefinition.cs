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
    protected void Supports<TStorage, TFeature>()
        where TStorage : StorageType
        where TFeature : Feature, new()
    {
        var storageType = StorageType.Get<TStorage>();
        if (storageToFeature.TryGetValue(storageType, out var feature))
        {
            throw new Exception($"Storage {storageType} is already supported by {feature.SupportedBy}");
        }

        storageToFeature.Add(new StorageFeature<TFeature>(storageType));
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

    internal IReadOnlyCollection<StorageType> GetSupportedStorages(IReadOnlyCollection<StorageType> selectedStorages) =>
        selectedStorages.Count > 0 ? selectedStorages : [.. storageToFeature.Select(x => x.StorageType)];

    readonly List<Action<SettingsHolder>> defaults = [];
    readonly StorageTypeToFeatureMap storageToFeature = [];

    class StorageTypeToFeatureMap : KeyedCollection<StorageType, IStorageFeature>
    {
        protected override StorageType GetKeyForItem(IStorageFeature item) => item.StorageType;
    }

    interface IStorageFeature
    {
        StorageType StorageType { get; }

        string SupportedBy { get; }

        void Apply(FeatureComponent.Settings settings);
    }
    class StorageFeature<TFeature>(StorageType storageType) : IStorageFeature
        where TFeature : Feature, new()
    {
        public string SupportedBy { get; } = Feature.GetFeatureName<TFeature>();
        public StorageType StorageType { get; } = storageType;
        public void Apply(FeatureComponent.Settings settings) => settings.EnableFeature<TFeature>();
    }
}