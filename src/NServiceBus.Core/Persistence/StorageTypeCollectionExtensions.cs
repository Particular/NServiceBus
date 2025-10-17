#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Linq;

static class StorageTypeCollectionExtensions
{
    extension(IReadOnlyCollection<StorageType>? storageTypes)
    {
        public bool Contains<TStorage>() where TStorage : StorageType
        {
            if (storageTypes is null)
            {
                return false;
            }

            var storageType = StorageType.Get<TStorage>();
            return storageTypes.Contains(storageType);
        }
    }
}