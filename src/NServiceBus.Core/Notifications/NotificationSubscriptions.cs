namespace NServiceBus.Notifications
{
    using System;
    using System.Collections.Generic;

    class NotificationSubscriptions
    {
        public NotificationSubscriptions()
        {
            subscriptions = new List<Action<BusNotifications>>();
        }

        public void ApplyTo(BusNotifications notifications)
        {
            subscriptions.ForEach(subscription => subscription(notifications));
        }

        List<Action<BusNotifications>> subscriptions;

        public void Register(Action<BusNotifications> action)
        {
            
            subscriptions.Add(action);
        }
    }
}