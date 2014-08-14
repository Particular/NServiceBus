#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.0", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
    public static class ConfigureInMemorySagaPersister
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.0", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
        public static Configure InMemorySagaPersister(this Configure config)
        {
            throw new InvalidOperationException();
        }
    }
}
