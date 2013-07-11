namespace NServiceBus
{
    using System;
    using System.Net.Mail;
    using Settings;

    /// <summary>
    /// Custom notification settings
    /// </summary>
    public class NotificationsSettings : ISetDefaultSettings
    {
        readonly Action<SmtpFailedRecipientsException> defaultCallback = _ => { };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NotificationsSettings()
        {
            SettingsHolder.SetDefault("Notifications.DeliveryFailuresCallback", defaultCallback);
        }

        /// <summary>
        /// Allows the user to specify a callback to be called when an e-mail is sent using an <see cref="T:System.Net.Mail.SmtpClient"/> and cannot be delivered to all recipients.
        /// </summary>
        /// <param name="callback">The callback to be used.</param>
        public void HandleDeliveryFailures(Action<SmtpFailedRecipientsException> callback)
        {
            SettingsHolder.Set("Notifications.DeliveryFailuresCallback", callback);
        }
    }
}