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
        /// <typeparam name="T"><see cref="StorageType"/></typeparam>
        protected void Supports<T>(Action<SettingsHolder> action) where T : StorageType
        {
            Guard.AgainstNull(action, "action");
            if (storageToActionMap.ContainsKey(typeof(T)))
            {
                throw new Exception(string.Format("Action for {0} already defined.", typeof(T)));
            }
            storageToActionMap[typeof(T)] = action;
        }

        /// <summary>
        /// Used be the storage definitions to declare what they support
        /// </summary>
        [ObsoleteEx(
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0",
           ReplacementTypeOrMember = "Supports<T>()")]
        protected void Supports(Storage storage, Action<SettingsHolder> action)
        {
            var storageType = StorageType.FromEnum(storage);
            if (storageToActionMap.ContainsKey(storageType))
            {
                throw new Exception(string.Format("Action for {0} already defined.", storage));
            }
            storageToActionMap[storageType] = action;
        }

        /// <summary>
        /// Used be the storage definitions to declare what they support
        /// </summary>
        protected void Defaults(Action<SettingsHolder> action)
        {
            Guard.AgainstNull(action, "action");
            defaults.Add(action);
        }

        /// <summary>
        /// True if supplied storage is supported
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "HasSupportFor<T>()")]
        public bool HasSupportFor(Storage storage)
        {
            return storageToActionMap.ContainsKey(StorageType.FromEnum(storage));
        }

        /// <summary>
        /// True if supplied storage is supported
        /// </summary>
        public bool HasSupportFor<T>() where T : StorageType
        {
            return HasSupportFor(typeof(T));
        }

        /// <summary>
        /// True if supplied storage is supported
        /// </summary>
        public bool HasSupportFor(Type storageType)
        {
            Guard.AgainstNull(storageType, "storageType");
            return storageToActionMap.ContainsKey(storageType);
        }

        internal void ApplyActionForStorage(Type storageType, SettingsHolder settings)
        {
            if (!storageType.IsSubclassOf(typeof(StorageType)))
            {
                throw new ArgumentException(string.Format("Storage type '{0}' is not a sub-class of StorageType", storageType.FullName), "storageType");
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

        List<Action<SettingsHolder>> defaults = new List<Action<SettingsHolder>>();
        Dictionary<Type, Action<SettingsHolder>> storageToActionMap = new Dictionary<Type, Action<SettingsHolder>>();
    }
}