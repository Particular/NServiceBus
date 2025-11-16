#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class CompositeNotification
{
    public void Register<TEvent>(INotificationSubscriptions<TEvent> notification) =>
        notifications.Add(new Notifier<TEvent>(notification));

    // Currently we only have class notifications and no structs or records so object is fine.
    public Task Raise(object @event, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < notifications.Count; i++)
        {
            if (notifications[i].Handle(@event))
            {
                return notifications[i].Raise(@event, cancellationToken);
            }
        }
        return Task.CompletedTask;
    }

    readonly List<INotifier> notifications = [];

    interface INotifier
    {
        bool Handle(object @event);
        Task Raise(object @event, CancellationToken cancellationToken = default);
    }

    class Notifier<TEvent>(INotificationSubscriptions<TEvent> notifier) : INotifier
    {
        public bool Handle(object @event) => @event is TEvent;

        public Task Raise(object @event, CancellationToken cancellationToken = default) =>
            notifier.Raise((TEvent)@event, cancellationToken);
    }
}