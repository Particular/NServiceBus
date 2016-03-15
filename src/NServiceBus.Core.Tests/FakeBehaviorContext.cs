namespace NServiceBus.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using ObjectBuilder;

    abstract class FakeBehaviorContext : IBehaviorContext
    {
        protected FakeBehaviorContext()
        {
            Extensions = new ContextBag();
            Builder = new FuncBuilder();
            eventAggregator = new FakeEventAggregator();
            Extensions.Set<IEventAggregator>(eventAggregator);
        }

        public ContextBag Extensions { get; }
        public IBuilder Builder { get; }

        public T GetNotification<T>()
        {
            return (T)NotificationsRaised.LastOrDefault(n => n is T);
        }

        protected List<object> NotificationsRaised => eventAggregator.NotificationsRaised;

        FakeEventAggregator eventAggregator;

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

            public List<object> NotificationsRaised { get; }
        }
    }
}