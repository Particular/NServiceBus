#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Persistence;

sealed class PersistenceRegistry
{
    public EnableBuilder<TDefinition> Enable<TDefinition>()
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        if (definitions.TryGetValue(typeof(TDefinition), out var persistenceAndStorageTypes))
        {
            return new EnableBuilder<TDefinition>((TDefinition)persistenceAndStorageTypes.Definition, this);
        }

        var definition = TDefinition.Create();
        definitions.Add(typeof(TDefinition), (definition, []));
        return new EnableBuilder<TDefinition>((TDefinition)persistenceAndStorageTypes.Definition, this);
    }

    public IReadOnlyCollection<EnabledPersistence> Merge()
    {
        IEnumerable<KeyValuePair<Type, (PersistenceDefinition Definition, List<StorageType> EnabledStorages)>> reversedDefinitions = definitions.Reverse();

        var availableStorages = new List<StorageType>(StorageType.GetAvailableStorageTypes());
        var enabledPersistences = new List<EnabledPersistence>();

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
                enabledPersistences.Add(new EnabledPersistence(selectedStorages, persistenceDefinition));
            }
        }

        return enabledPersistences;
    }

    void EnableStorageFor<TDefinition>(StorageType storage)
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        var builder = Enable<TDefinition>();

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

    void EnableStorageFor<TDefinition, TStorage>()
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
        where TStorage : StorageType =>
        EnableStorageFor<TDefinition>(StorageType.Get<TStorage>());

    public class EnableBuilder<TDefinition>(
        TDefinition definition,
        PersistenceRegistry registry)
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        public TDefinition Persistence { get; } = definition;

        public void WithStorage<TStorage>()
            where TStorage : StorageType =>
            registry.EnableStorageFor<TDefinition, TStorage>();

        public void WithStorage(StorageType storageType) => registry.EnableStorageFor<TDefinition>(storageType);
    }

    readonly Dictionary<Type, (PersistenceDefinition Definition, List<StorageType> EnabledStorageTypes)> definitions = [];
}