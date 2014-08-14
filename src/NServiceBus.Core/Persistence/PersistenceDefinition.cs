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
            if (StorageToActionMap.ContainsKey(storage))
            {
                throw new Exception(string.Format("Action for {0} already defined.", storage));
            }
            StorageToActionMap[storage] = action;
        }

        /// <summary>
        /// True if supplied storage is supported
        /// </summary>
        public bool HasSupportFor(Storage storage)
        {
            return StorageToActionMap.ContainsKey(storage);
        }

        internal void ApplyActionForStorage(Storage storage, SettingsHolder settings)
        {
            var actionForStorage = StorageToActionMap[storage];
            actionForStorage(settings);
        }

        internal Storage[] GetSupportedStorages(Storage[] selectedStorages)
        {
            if (selectedStorages.Length > 0)
            {
                return selectedStorages;
            }

            return StorageToActionMap.Keys.ToArray();
        }

        Dictionary<Storage, Action<SettingsHolder>> StorageToActionMap = new Dictionary<Storage, Action<SettingsHolder>>();
    }
}