#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Settings;

static class PersistenceComponentExtensions
{
    internal static bool HasSupportFor<T>(this IReadOnlySettings settings) where T : StorageType
    {
        _ = settings.TryGet(out IReadOnlyCollection<StorageType> supportedStorages);

        return supportedStorages.Contains<T>();
    }
}