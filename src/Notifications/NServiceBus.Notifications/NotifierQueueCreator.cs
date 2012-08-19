namespace NServiceBus.Notifications
{
    using Unicast.Queuing;
    using Config;

    /// <summary>
    /// Signals to create a notification queue
    /// </summary>
    public class NotifierQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Notification queue.
        /// </summary>
        public Address Address
        {
            get { return BusExtensions.NotificationAddress; }
        }

        /// <summary>
        /// Disable the creation of the Notification queue
        /// </summary>
        public bool IsDisabled
        {
            get { return ConfigureNotifications.NotificationsDisabled; }
        }
    }
}