namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods for <see cref="ConfigurationBuilder"/> that expose Queue creation settings.
    /// </summary>
    public static partial class ConfigureQueueCreation
    {
        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        public static void DoNotCreateQueues(this ConfigurationBuilder config)
        {
            config.Settings.Set("Transport.CreateQueues", false);
        }

        /// <summary>
        /// Gets whether or not queues should be created.
        /// </summary>
        public static bool CreateQueues(this Configure config)
        {
            bool createQueues;
            if (config.Settings.TryGet("Transport.CreateQueues", out createQueues))
            {
                return createQueues;
            }
            return true;
        }
    }
}
