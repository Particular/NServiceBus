namespace NServiceBus
{
    /// <summary>
    /// Extension methods to configure notifications,
    /// </summary>
    public static class ConfigureNotifications
    {
        /// <summary>
        /// Disables notifications.
        /// </summary>
        /// <param name="config">The <see cref="NServiceBus.Configure"/>.</param>
        /// <returns>The <see cref="NServiceBus.Configure"/>.</returns>
        public static Configure DisableNotifications(this Configure config)
        {
            NotificationsDisabled = true;
            return config;
        }

        internal static bool NotificationsDisabled { get; set; }
    }
}