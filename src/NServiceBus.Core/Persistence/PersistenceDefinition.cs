namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Settings;

    /// <summary>
    /// Base class for persistence definitions
    /// </summary>
    public abstract class PersistenceDefinition
    {
        /// <summary>
        /// Used be the storage definitions to declare what they support
        /// </summary>
        protected void Supports(Storage storage, Action<SettingsHolder> action)
        {
            if (storageToActionMap.ContainsKey(storage))
            {
                throw new Exception(string.Format("Action for {0} already defined.", storage));
            }
            storageToActionMap[storage] = action;
        }

        /// <summary>
        /// Used be the storage definitions to declare what they support
        /// </summary>
        protected void Defaults(Action<SettingsHolder> action)
        {
            defaults.Add(action);
        }

        /// <summary>
        /// True if supplied storage is supported
        /// </summary>
        public bool HasSupportFor(Storage storage)
        {
            return storageToActionMap.ContainsKey(storage);
        }

        internal void ApplyActionForStorage(Storage storage, SettingsHolder settings)
        {
            var actionForStorage = storageToActionMap[storage];
            actionForStorage(settings);
        }

        internal void ApplyDefaults(SettingsHolder settings)
        {
            foreach (var @default in defaults)
            {
                @default(settings);
            }
        }

        internal List<Storage> GetSupportedStorages(List<Storage> selectedStorages)
        {
            if (selectedStorages.Count > 0)
            {
                return selectedStorages;
            }

            return storageToActionMap.Keys.ToList();
        }

        List<Action<SettingsHolder>> defaults = new List<Action<SettingsHolder>>();
        Dictionary<Storage, Action<SettingsHolder>> storageToActionMap = new Dictionary<Storage, Action<SettingsHolder>>();
    }
}