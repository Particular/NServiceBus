#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Linq;

static class StorageTypeCollectionExtensions
{
    extension(IReadOnlyCollection<(StorageType Storage, StorageType.Options Options)>? storageTypes)
    {
        public bool Contains<TStorage>() where TStorage : StorageType, new()
        {
            if (storageTypes is null)
            {
                return false;
            }

            var storageType = new TStorage();
            return storageTypes.Any(s => s.Storage.Equals(storageType));
        }

        public TOptions? Get<TOptions>()
            where TOptions : StorageType.Options =>
            storageTypes?.Select(s => s.Options).OfType<TOptions>().SingleOrDefault();
    }
}