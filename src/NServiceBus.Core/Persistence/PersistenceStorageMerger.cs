namespace NServiceBus;

using System.Collections.Generic;
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
                SelectedStorages = []
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

            if (currentDefinition.SelectedStorages.Count != 0)
            {
                mergedEnabledPersistences.Add(currentDefinition);
            }
        }

        return mergedEnabledPersistences;
    }
}