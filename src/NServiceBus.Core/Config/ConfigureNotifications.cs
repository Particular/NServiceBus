namespace NServiceBus.Config
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
        [ObsoleteEx(Message = "Moved to NServiceBus namespace.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableNotifications(this Configure config)
        {
            NServiceBus.ConfigureNotifications.NotificationsDisabled = true;
            return config;
        }
    }
}

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