namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class NotificationSubscriptions
    {
        public IEnumerable<ISubscription> Get<T>()
        {
            if (!subscriptions.TryGetValue(typeof(T), out var subscribers))
            {
                return Enumerable.Empty<ISubscription>();
            }

            return subscribers;
        }

        public void Subscribe<T>(Func<T, Task> subscription)
        {
            var eventType = typeof(T);

            if (!subscriptions.TryGetValue(eventType, out var currentSubscriptions))
            {
                currentSubscriptions = new List<ISubscription>();
                subscriptions[eventType] = currentSubscriptions;
            }

            currentSubscriptions.Add(new Subscription<T>(subscription));
        }

        Dictionary<Type, List<ISubscription>> subscriptions = new Dictionary<Type, List<ISubscription>>();


        public interface ISubscription
        {
            Task Invoke(object @event);
        }

        class Subscription<T> : ISubscription
        {
            public Subscription(Func<T, Task> invocation)
            {
                this.invocation = invocation;
            }

            public Task Invoke(object @event)
            {
                return invocation((T)@event);
            }

            Func<T, Task> invocation;
        }
    }
}