namespace NServiceBus
{
    using NServiceBus.Settings;

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
            config.settings.Set("Transport.CreateQueues", false);
        }

        /// <summary>
        /// Gets whether or not queues should be created
        /// </summary>
        internal static bool GetCreateQueues(this ReadOnlySettings settings)
        {
            bool createQueues;
            if (settings.TryGet("Transport.CreateQueues", out createQueues))
            {
                return createQueues;
            }
            return true;
        }
    }
}
