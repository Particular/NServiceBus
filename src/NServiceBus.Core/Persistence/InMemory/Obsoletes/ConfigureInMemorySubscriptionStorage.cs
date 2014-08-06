namespace NServiceBus
{
    using Persistence;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
    public static class ConfigureInMemorySubscriptionStorage
    {
        /// <summary>
        /// Stores subscription data in memory.
        /// This storage are for development scenarios only
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.InMemory>()")]
        public static Configure InMemorySubscriptionStorage(this Configure config)
        {
            return config.UsePersistence<Persistence.InMemory>();
        }
    }
}