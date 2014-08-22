namespace NServiceBus
{
    /// <summary>
    /// Configures purging
    /// </summary>
    public static partial class ConfigurePurging
    {
        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        public static void PurgeOnStartup(this BusConfiguration config, bool value)
        {
            config.Settings.Set("Transport.PurgeOnStartup", value);
        }

        /// <summary>
        /// Retrieves whether to purge the queues at startup or not.
        /// </summary>
        public static bool PurgeOnStartup(this Configure config)
        {
            bool purgeOnStartup;
            if (config.Settings.TryGet("Transport.PurgeOnStartup", out purgeOnStartup))
            {
                return purgeOnStartup;
            }
            return false;
        }
    }
}
