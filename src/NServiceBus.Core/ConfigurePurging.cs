namespace NServiceBus
{
    /// <summary>
    /// Configures purging
    /// </summary>
    public static class ConfigurePurging
    {
        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        public static Configure PurgeOnStartup(this Configure config, bool value)
        {
            PurgeRequested = value;

            return config;
        }

        /// <summary>
        /// True if the users wants the input queue to be purged when we starts up
        /// </summary>
        public static bool PurgeRequested { get; private set; }
    }
}