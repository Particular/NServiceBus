namespace NServiceBus.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class FakeEventAggregator : IEventAggregator
    {
        public FakeEventAggregator()
        {
            NotificationsRaised = new List<object>();
        }
        public Task Raise<T>(T @event)
        {
            NotificationsRaised.Add(@event);
            return TaskEx.CompletedTask;
        }

        public T GetNotification<T>()
        {
            return (T)NotificationsRaised.LastOrDefault(n => n is T);
        }

        public List<object> NotificationsRaised { get; }
    }
}