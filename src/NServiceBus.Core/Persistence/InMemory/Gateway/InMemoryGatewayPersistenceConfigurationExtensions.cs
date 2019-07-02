namespace NServiceBus
{
    using Features;

    /// <summary>
    /// Configuration options for the in memory gateway persistence.
    /// </summary>
    public static class InMemoryGatewayPersistenceConfigurationExtensions
    {
        /// <summary>
        /// Configures the size of the LRU cache.
        /// </summary>
        /// <param name="persistenceExtensions">The persistence extensions to extend.</param>
        /// <param name="maxSize">Maximum size of the LRU cache.</param>
        public static void GatewayDeduplicationCacheSize(this PersistenceExtensions<InMemoryPersistence> persistenceExtensions, int maxSize)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNegativeAndZero(nameof(maxSize), maxSize);
            persistenceExtensions.Settings.Set(InMemoryGatewayPersistence.MaxSizeKey, maxSize);
        }
    }
}