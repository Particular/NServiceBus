namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Persistence;
using Settings;

sealed class PersistenceRegistry
{
    public EnableBuilder<TDefinition> Enable<TDefinition>(SettingsHolder settings)
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        if (definitions.TryGetValue(typeof(TDefinition), out var persistenceAndStorageTypes))
        {
            return new EnableBuilder<TDefinition>((TDefinition)persistenceAndStorageTypes.Definition, this, settings);
        }

        var definition = TDefinition.Create(settings);
        definitions.Add(typeof(TDefinition), (definition, []));
        return new EnableBuilder<TDefinition>((TDefinition)persistenceAndStorageTypes.Definition, this, settings);
    }

    public IReadOnlyCollection<MergedPersistence> Merge()
    {
        IEnumerable<KeyValuePair<Type, (PersistenceDefinition Definition, List<StorageType> EnabledStorages)>> reversedDefinitions = definitions.Reverse();

        var availableStorages = new List<StorageType>(StorageType.GetAvailableStorageTypes());
        var mergedEnabledPersistences = new List<MergedPersistence>();

        foreach (var (_, (persistenceDefinition, enabledStorages)) in reversedDefinitions)
        {
            var supportedStorages = persistenceDefinition.GetSupportedStorages(enabledStorages);

            var selectedStorages = new List<StorageType>();

            foreach (var storageType in supportedStorages)
            {
                if (!availableStorages.Contains(storageType))
                {
                    continue;
                }

                selectedStorages.Add(storageType);
                availableStorages.Remove(storageType);
            }

            if (selectedStorages.Count != 0)
            {
                mergedEnabledPersistences.Add(new MergedPersistence(selectedStorages, persistenceDefinition));
            }
        }

        return mergedEnabledPersistences;
    }

    void EnableStorageFor<TDefinition>(SettingsHolder settings, StorageType storage)
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        var builder = Enable<TDefinition>(settings);

        if (!builder.Persistence.HasSupportFor(storage))
        {
            throw new Exception($"{typeof(TDefinition).Name} does not support storage type {storage.GetType().Name}. See http://docs.particular.net/nservicebus/persistence-in-nservicebus for supported variations.");
        }

        var (_, enabledStorageTypes) = definitions[typeof(TDefinition)];
        if (!enabledStorageTypes.Contains(storage))
        {
            enabledStorageTypes.Add(storage);
        }
    }

    void EnableStorageFor<TDefinition, TStorage>(SettingsHolder settings)
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
        where TStorage : StorageType =>
        EnableStorageFor<TDefinition>(settings, StorageType.Get<TStorage>());

    public class EnableBuilder<TDefinition>(
        TDefinition definition,
        PersistenceRegistry registry,
        SettingsHolder settings)
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        public TDefinition Persistence { get; } = definition;

        public void WithStorage<TStorage>()
            where TStorage : StorageType =>
            registry.EnableStorageFor<TDefinition, TStorage>(settings);

        public void WithStorage(StorageType storageType) => registry.EnableStorageFor<TDefinition>(settings, storageType);
    }

    readonly Dictionary<Type, (PersistenceDefinition Definition, List<StorageType> EnabledStorageTypes)> definitions = [];
}