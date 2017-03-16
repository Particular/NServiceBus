namespace NServiceBus.Persistence.Legacy
{
    using Settings;

    /// <summary>
    /// Provides configuration extensions when using <see cref="MsmqPersistence"/>.
    /// </summary>
    public static class MsmqSubscriptionStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures the queue used to store subscriptions.
        /// </summary>
        /// <param name="persistenceExtensions">The settings to extend.</param>
        /// <param name="queue">The queue name.</param>
        public static void SubscriptionQueue(this PersistenceExtensions<MsmqPersistence> persistenceExtensions, string queue)
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