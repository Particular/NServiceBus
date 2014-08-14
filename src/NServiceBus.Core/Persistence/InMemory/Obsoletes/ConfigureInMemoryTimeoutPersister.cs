#pragma warning disable 1591
// ReSharper disable once UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
    public static class ConfigureInMemoryTimeoutPersister
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}