namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to <see cref="EndpointConfiguration" />.
    /// </summary>
    public static class ConfigureError
    {
        /// <summary>
        /// Configure error queue settings.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="errorQueue">The name of the error queue to use.</param>
        public static void SendFailedMessagesTo(this EndpointConfiguration config, string errorQueue)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentException.ThrowIfNullOrWhiteSpace(errorQueue);

            config.Settings.Set(ErrorQueueSettings.SettingsKey, errorQueue);
        }
    }
}