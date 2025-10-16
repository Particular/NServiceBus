namespace NServiceBus;

using System.Collections.Generic;
using Persistence;

record EnabledPersistence(IReadOnlyCollection<StorageType> SelectedStorages, PersistenceDefinition Definition);