namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Notification<TEvent> : INotificationSubscriptions<TEvent>
{
    public void Subscribe(Func<TEvent, CancellationToken, Task> subscription) => subscriptions.Add(subscription);

    Task INotificationSubscriptions<TEvent>.Raise(TEvent @event, CancellationToken cancellationToken) =>
        Task.WhenAll(subscriptions.Select(s => s.Invoke(@event, cancellationToken)));

    readonly List<Func<TEvent, CancellationToken, Task>> subscriptions = [];
}