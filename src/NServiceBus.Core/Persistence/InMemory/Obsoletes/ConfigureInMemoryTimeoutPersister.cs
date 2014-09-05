#pragma warning disable 1591
// ReSharper disable once UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "Use configuration.UsePersistence<InMemoryPersistence>(), where configuration is an instance of type BusConfiguration")]
    public static class ConfigureInMemoryTimeoutPersister
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "Use configuration.UsePersistence<InMemoryPersistence>(), where configuration is an instance of type BusConfiguration")]
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}