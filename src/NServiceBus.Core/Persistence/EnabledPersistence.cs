#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Persistence;

record EnabledPersistence(IReadOnlyCollection<(StorageType Storage, StorageType.Options Options)> SelectedStorages, PersistenceDefinition Definition);