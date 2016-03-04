namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Persistence;
    using Settings;

    class PersistenceStorageMerger
    {
        public static List<EnabledPersistence> Merge(List<EnabledPersistence> definitions, SettingsHolder settings)
        {
            definitions.Reverse();

            var availableStorages = StorageType.GetAvailableStorageTypes();
            var mergedEnabledPersistences = new List<EnabledPersistence>();

            foreach (var definition in definitions)
            {
                var persistenceDefinition = definition.DefinitionType.Construct<PersistenceDefinition>();
                var supportedStorages = persistenceDefinition.GetSupportedStorages(definition.SelectedStorages);

                var currentDefinition = new EnabledPersistence
                {
                    DefinitionType = definition.DefinitionType,
                    SelectedStorages = new List<Type>()
                };

                foreach (var storageType in supportedStorages)
                {
                    if (availableStorages.Contains(storageType))
                    {
                        currentDefinition.SelectedStorages.Add(storageType);
                        availableStorages.Remove(storageType);
                        persistenceDefinition.ApplyActionForStorage(storageType, settings);
                    }
                }

                if (currentDefinition.SelectedStorages.Any())
                {
                    mergedEnabledPersistences.Add(currentDefinition);
                }
            }

            return mergedEnabledPersistences;
        }
    }
}