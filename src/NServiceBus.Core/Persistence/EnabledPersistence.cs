namespace NServiceBus;

using System;
using System.Collections.Generic;
using Persistence;

class EnabledPersistence
{
    public List<StorageType> SelectedStorages { get; set; }
    public Type DefinitionType;
}

record MergedPersistence(IReadOnlyCollection<StorageType> SelectedStorages, PersistenceDefinition Definition);