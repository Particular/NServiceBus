namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods for <see cref="BusConfiguration"/> that expose Queue creation settings.
    /// </summary>
    public static class ConfigureQueueCreation
    {
        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        public static void DoNotCreateQueues(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            config.Settings.Set("Transport.CreateQueues", false);
        }

        /// <summary>
        /// Gets whether or not queues should be created.
        /// </summary>
        public static bool CreateQueues(this Configure config)
        {
            Guard.AgainstNull(config, "config");
            bool createQueues;
            if (config.Settings.TryGet("Transport.CreateQueues", out createQueues))
            {
                return createQueues;
            }
            return true;
        }
    }
}
