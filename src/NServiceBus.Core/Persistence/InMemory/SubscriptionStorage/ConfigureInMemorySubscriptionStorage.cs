namespace NServiceBus
{
    using Persistence.InMemory.SubscriptionStorage;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureInMemorySubscriptionStorage
    {
        /// <summary>
        /// Stores subscription data in memory.
        /// This storage are for development scenarios only
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure InMemorySubscriptionStorage(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);
            return config;
        }
    }
}