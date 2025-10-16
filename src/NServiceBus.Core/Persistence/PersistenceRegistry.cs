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
        if (definitions.TryGetValue(typeof(TDefinition), out var builder))
        {
            return (EnableBuilder<TDefinition>)builder;
        }

        var strongBuilder = new EnableBuilder<TDefinition>();
        definitions.Add(typeof(TDefinition), strongBuilder);
        return strongBuilder;
    }

    public IReadOnlyCollection<EnabledPersistence> Merge()
    {
        IEnumerable<KeyValuePair<Type, IEnabledBuilder>> builtPersistences = definitions.Reverse();

        var availableStorages = new List<StorageType>(StorageType.GetAvailableStorageTypes());
        var enabledPersistences = new List<EnabledPersistence>();

        foreach (var (_, builtPersistence) in builtPersistences)
        {
            var supportedStorages = builtPersistence.Persistence.GetSupportedStorages(builtPersistence.EnabledStorageTypes);

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
                enabledPersistences.Add(new EnabledPersistence(selectedStorages, builtPersistence.Persistence));
            }
        }

        return enabledPersistences;
    }

    public class EnableBuilder<TDefinition> : IEnabledBuilder
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        public PersistenceDefinition Persistence { get; } = TDefinition.Create();

        public IReadOnlyCollection<StorageType> EnabledStorageTypes => enabledStorageTypes;

        public void WithStorage<TStorage>() where TStorage : StorageType => WithStorage(StorageType.Get<TStorage>());

        public void WithStorage(StorageType storageType)
        {
            if (!Persistence.HasSupportFor(storageType))
            {
                throw new Exception($"{typeof(TDefinition).Name} does not support storage type '{storageType}'. See http://docs.particular.net/nservicebus/persistence-in-nservicebus for supported variations.");
            }

            enabledStorageTypes.Add(storageType);
        }

        readonly HashSet<StorageType> enabledStorageTypes = [];
    }

    interface IEnabledBuilder
    {
        PersistenceDefinition Persistence { get; }

        IReadOnlyCollection<StorageType> EnabledStorageTypes { get; }
    }

    readonly Dictionary<Type, IEnabledBuilder> definitions = [];
}