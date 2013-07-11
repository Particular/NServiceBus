namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureQueueCreation
    {
        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DoNotCreateQueues(this Configure config)
        {
            DontCreateQueues = true;

            return config;
        }

        internal static bool DontCreateQueues { get; private set; }
    }
}
