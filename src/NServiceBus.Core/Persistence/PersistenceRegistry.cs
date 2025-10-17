#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Persistence;

sealed class PersistenceRegistry
{
    public EnableBuilder<TDefinition> Enable<TDefinition>()
        where TDefinition : PersistenceDefinition, IPersistenceDefinitionFactory<TDefinition>
    {
        if (tracker.TryGetValue(typeof(TDefinition), out var position))
        {
            return (EnableBuilder<TDefinition>)definitions[position];
        }

        var strongBuilder = new EnableBuilder<TDefinition>();
        // using the count here works because we never remove enabled persistences
        tracker.Add(typeof(TDefinition), definitions.Count);
        definitions.Add(strongBuilder);
        return strongBuilder;
    }

    public IReadOnlyCollection<EnabledPersistence> Merge()
    {
        // the order of the definitions is reversed when merging because the last UsePersistence calls have higher priority
        // taking a copy to avoid modifying the original list
        var builtPersistences = new List<IEnabledBuilder>(definitions);
        builtPersistences.Reverse();

        var availableStorages = new List<StorageType>(StorageType.GetAvailableStorageTypes());
        var enabledPersistences = new List<EnabledPersistence>();

        foreach (var builtPersistence in builtPersistences)
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

    readonly Dictionary<Type, int> tracker = [];
    // using a list to preserve the order of registrations since a dictionary does not guarantee it
    // the order is important because the last UsePersistence calls have higher priority during merging
    // that's why the list is reversed when merging.
    readonly List<IEnabledBuilder> definitions = [];
}