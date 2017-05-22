namespace NServiceBus
{
    using Settings;

    /// <summary>
    /// Contains extension methods for <see cref="EndpointConfiguration" /> that expose Queue creation settings.
    /// </summary>
    public static class ConfigureQueueCreation
    {
        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void DoNotCreateQueues(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.Settings.Set("Transport.CreateQueues", false);
        }

        /// <summary>
        /// Gets whether or not queues should be created.
        /// </summary>
        public static bool CreateQueues(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            bool createQueues;
            return !settings.TryGet("Transport.CreateQueues", out createQueues) || createQueues;
        }
    }
}