namespace NServiceBus
{
    using NServiceBus.Settings;

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
        public static void PurgeOnStartup(this ConfigurationBuilder config, bool value)
        {
            config.Settings.Set("Transport.PurgeOnStartup", value);
        }

        /// <summary>
        /// Retrieves the current stored value for Transport.PurgeOnStartup stored in <paramref name="settings"/>.
        /// </summary>
        public static bool GetPurgeOnStartup(ReadOnlySettings settings)
        {
            bool purgeOnStartup;
            if (settings.TryGet("Transport.PurgeOnStartup", out purgeOnStartup))
            {
                return purgeOnStartup;
            }
            return false;
        }
    }
}
