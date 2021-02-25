namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    class Notification<TEvent> : INotificationSubscriptions<TEvent>
    {
        public void Subscribe(Func<TEvent, CancellationToken, Task> subscription)
        {
            subscriptions.Add(subscription);
        }

        List<Func<TEvent, CancellationToken, Task>> subscriptions = new List<Func<TEvent, CancellationToken, Task>>();

#pragma warning disable CS1066 // The default value specified will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
        Task INotificationSubscriptions<TEvent>.Raise(TEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(subscriptions.Select(s => s.Invoke(@event, cancellationToken)));
        }
#pragma warning restore CS1066 // The default value specified will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
    }
}