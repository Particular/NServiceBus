namespace NServiceBus.Persistence.Legacy
{
    using Settings;

    /// <summary>
    /// Provides configuration extensions when using <see cref="MsmqPersistence"/>.
    /// </summary>
    public static class MsmqSubscriptionStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures the queue to store subscriptions by <see cref="MsmqPersistence"/>.
        /// </summary>
        /// <param name="persistenceExtensions">The settings to extend.</param>
        /// <param name="queue">The queue name.</param>
        public static void SubscriptionQueue<T>(this PersistenceExtensions<T> persistenceExtensions, string queue) 
            where T : MsmqPersistence
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(queue), queue);

            persistenceExtensions.Settings.Set(msmqPersistenceQueueConfigurationKey, queue);
        }

        /// <summary>
        /// Configures the queue to store subscriptions by <see cref="MsmqPersistence"/>.
        /// </summary>
        /// <param name="persistenceExtensions">The settings to extend.</param>
        /// <param name="queue">The queue name.</param>
        public static void SubscriptionQueue<T, S>(this PersistenceExtensions<T, S> persistenceExtensions, string queue) 
            where T : MsmqPersistence 
            where S : StorageType
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(queue), queue);

            persistenceExtensions.Settings.Set(msmqPersistenceQueueConfigurationKey, queue);
        }

        internal static string GetConfiguredMsmqPersistenceSubscriptionQueue(this ReadOnlySettings settings)
        {
            return settings.GetOrDefault<string>(msmqPersistenceQueueConfigurationKey);
        }

        const string msmqPersistenceQueueConfigurationKey = "MsmqSubscriptionPersistence.QueueName";
    }
}