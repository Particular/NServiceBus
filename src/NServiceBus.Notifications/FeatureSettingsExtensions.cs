namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// Feature extensions for notifications.
    /// </summary>
    public static class FeatureSettingsExtensions
    {
        /// <summary>
        /// Customise notifications.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="customSettings"></param>
        /// <returns></returns>
        public static FeatureSettings Notifications(this FeatureSettings settings, Action<NotificationsSettings> customSettings)
        {
            customSettings(new NotificationsSettings());

            return settings;
        }
    }
}