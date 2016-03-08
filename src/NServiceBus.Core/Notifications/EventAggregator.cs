namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;

    class EventAggregator : IEventAggregator
    {
        public EventAggregator(NotificationSubscriptions subscriptions)
        {
            this.subscriptions = subscriptions;
        }

        public Task Raise<T>(T @event)
        {
            return Task.WhenAll(subscriptions.Get<T>().Select(s => s.Invoke(@event)));
        }

        NotificationSubscriptions subscriptions;
    }
}