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

        return count switch
        {
            0 => Task.CompletedTask,
            1 => subscriptions[0].Invoke(@event, cancellationToken),
            _ => Task.WhenAll(NotifyMultiple(subscriptions, count, @event, cancellationToken))
        };

        static Task[] NotifyMultiple(List<Func<TEvent, CancellationToken, Task>> subscriptions, int count, TEvent @event, CancellationToken cancellationToken)
        {
            var tasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                tasks[i] = subscriptions[i].Invoke(@event, cancellationToken);
            }
            return tasks;
        }
    }

    readonly List<Func<TEvent, CancellationToken, Task>> subscriptions = [];
}