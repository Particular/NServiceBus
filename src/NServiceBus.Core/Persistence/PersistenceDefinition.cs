namespace NServiceBus.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using Settings;

/// <summary>
/// Base class for persistence definitions.
/// </summary>
public abstract class PersistenceDefinition
{
    /// <summary>
    /// Used by the storage definitions to declare what they support.
    /// </summary>
    protected void Supports<T>(Action<SettingsHolder> action) where T : StorageType
    {
        ArgumentNullException.ThrowIfNull(action);
        if (storageToActionMap.ContainsKey(typeof(T)))
        {
            throw new Exception($"Action for {typeof(T)} already defined.");
        }
        storageToActionMap[typeof(T)] = action;
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
    public bool HasSupportFor<T>() where T : StorageType
    {
        return HasSupportFor(typeof(T));
    }

    /// <summary>
    /// True if supplied storage is supported.
    /// </summary>
    public bool HasSupportFor(Type storageType)
    {
        ArgumentNullException.ThrowIfNull(storageType);
        return storageToActionMap.ContainsKey(storageType);
    }

    /// <summary>
    /// Returns infrastructure information about the persistence.
    /// </summary>
    /// <param name="settings">Settings configured for the endpoint.</param>
    /// <returns>Persistence manifest.</returns>
    public virtual IEnumerable<KeyValuePair<string, ManifestItem>> GetManifest(SettingsHolder settings) => Enumerable.Empty<KeyValuePair<string, ManifestItem>>();

    internal void ApplyActionForStorage(Type storageType, SettingsHolder settings)
    {
        if (!storageType.IsSubclassOf(typeof(StorageType)))
        {
            throw new ArgumentException($"Storage type '{storageType.FullName}' is not a sub-class of StorageType", nameof(storageType));
        }
        var actionForStorage = storageToActionMap[storageType];
        actionForStorage(settings);
    }

    internal void ApplyDefaults(SettingsHolder settings)
    {
        foreach (var @default in defaults)
        {
            @default(settings);
        }
    }

    internal List<Type> GetSupportedStorages(List<Type> selectedStorages)
    {
        if (selectedStorages.Count > 0)
        {
            return selectedStorages;
        }

        return storageToActionMap.Keys.ToList();
    }

    readonly List<Action<SettingsHolder>> defaults = [];
    readonly Dictionary<Type, Action<SettingsHolder>> storageToActionMap = [];
}