namespace NServiceBus
{
    using System;

#pragma warning disable 1591
    public static class ConfigureInMemoryTimeoutPersister
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
// ReSharper disable once UnusedParameter.Global
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
#pragma warning restore 1591
}