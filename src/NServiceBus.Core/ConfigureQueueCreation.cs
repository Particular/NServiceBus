namespace NServiceBus
{
    using System.ComponentModel;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureQueueCreation
    {
        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        public static Configure DoNotCreateQueues(this Configure config)
        {
            DontCreateQueues = true;

            return config;
        }

        /// <summary>
        /// Gets whether or not queues should be created
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static bool DontCreateQueues { get; private set; }
    }
}
