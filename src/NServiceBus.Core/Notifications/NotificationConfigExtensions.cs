namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Notifications;

    /// <summary>
    /// Provides config options for the notifications
    /// </summary>
    public static class NotificationConfigExtensions
    {
        /// <summary>
        /// Allows users to subscribe to bus notifications
        /// </summary>
        public static void Notifications(this BusConfiguration configuration, Action<BusNotifications> action)
        {
            var settings = configuration.GetSettings();

            NotificationSubscriptions subscriptions;

            if (!settings.TryGet(out subscriptions))
            {
                subscriptions = new NotificationSubscriptions();

                settings.Set<NotificationSubscriptions>(subscriptions);
            }

            subscriptions.Register(action);
        }
    }
}