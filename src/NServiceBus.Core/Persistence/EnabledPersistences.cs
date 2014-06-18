namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class EnabledPersistences
    {
        public bool HasSupportFor(Storage storageType)
        {
            return selection.Values.SelectMany(s => s.StoragesToEnable).Contains(storageType);
        }

        public void AddWildcardRegistration(Type definitionType, List<Storage> supportedStorages)
        {
            var alreadyClaimed = selection.Values.SelectMany(s => s.StoragesToEnable).ToList();

            AddOrUpdate(definitionType, supportedStorages.Where(s=>!alreadyClaimed.Contains(s)).ToList());
        }

        public void ClaimStorages(Type definitionType, List<Storage> storagesToEnable)
        {
            var distinctStoragesToEnable = storagesToEnable.Distinct().ToList();

            foreach (var selectedPersistence in selection.Values)
            {
                foreach (var storageToEnable in distinctStoragesToEnable)
                {
                    if (selectedPersistence.PersitenceType != definitionType && selectedPersistence.StoragesToEnable.Contains(storageToEnable))
                    {
                        throw new Exception(string.Format("Failed to enable storage for {0} provided by persistence {1} since its already been enabled for persistence {2}", storageToEnable, definitionType.Name, selectedPersistence.PersitenceType.Name));
                    }
                }

            }


            AddOrUpdate(definitionType, distinctStoragesToEnable);
        }

        void AddOrUpdate(Type definitionType, List<Storage> distinctStoragesToEnable)
        {
            EnabledPersistence existingSelection;
            if (selection.TryGetValue(definitionType, out existingSelection))
            {
                existingSelection.StoragesToEnable.AddRange(distinctStoragesToEnable);
                existingSelection.StoragesToEnable = existingSelection.StoragesToEnable.Distinct().ToList();
            }
            else
            {
                selection[definitionType] = new EnabledPersistence
                {
                    PersitenceType = definitionType,
                    StoragesToEnable = distinctStoragesToEnable
                };
            }
        }

        public IEnumerable<EnabledPersistence> GetEnabled()
        {
            return selection.Values.ToList();
        }



        Dictionary<Type, EnabledPersistence> selection = new Dictionary<Type, EnabledPersistence>();

        public class EnabledPersistence
        {
            public Type PersitenceType;
            public List<Storage> StoragesToEnable = new List<Storage>();
        }

    }
}