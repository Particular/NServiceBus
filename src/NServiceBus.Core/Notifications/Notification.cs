#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Notification<TEvent> : INotificationSubscriptions<TEvent>
{
    public void Subscribe(Func<TEvent, CancellationToken, Task> subscription) => subscriptions.Add(subscription);

    Task INotificationSubscriptions<TEvent>.Raise(TEvent @event, CancellationToken cancellationToken)
    {
        int count = subscriptions.Count;

        if (count == 0)
        {
            return Task.CompletedTask;
        }

        if (count == 1)
        {
            return subscriptions[0].Invoke(@event, cancellationToken);
        }

        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            tasks[i] = subscriptions[i].Invoke(@event, cancellationToken);
        }
        return Task.WhenAll(tasks);
    }

    readonly List<Func<TEvent, CancellationToken, Task>> subscriptions = [];
}