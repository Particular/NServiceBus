namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class Notification<TEvent> : INotificationSubscriptions<TEvent>
    {
        public void Subscribe(Func<TEvent, Task> subscription)
        {
            subscriptions.Add(subscription);
        }

        List<Func<TEvent, Task>> subscriptions = new List<Func<TEvent, Task>>();

        Task INotificationSubscriptions<TEvent>.Raise(TEvent @event)
        {
            return Task.WhenAll(subscriptions.Select(s => s.Invoke(@event)));
        }
    }
}