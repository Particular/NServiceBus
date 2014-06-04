namespace NServiceBus
{
    using Persistence;

    public static class ConfigureInMemoryTimeoutPersister
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
        public static Configure UseInMemoryTimeoutPersister(this Configure config)
        {
            return config.UsePersistence<Persistence.InMemory>();
        }
    }
}