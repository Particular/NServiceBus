namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class CompositeNotification
    {
        public void Register<TEvent>(INotificationSubscriptions<TEvent> notification) => notifications.Add(typeof(TEvent), notification);

        public Task Raise<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event is null)
            {
                return Task.CompletedTask;
            }

            if (notifications.TryGetValue(typeof(TEvent), out var notification) && notification is INotificationSubscriptions<TEvent> notifier)
            {
                return notifier.Raise(@event, cancellationToken);
            }
            return Task.CompletedTask;
        }

        Dictionary<Type, object> notifications = new Dictionary<Type, object>();
    }
}