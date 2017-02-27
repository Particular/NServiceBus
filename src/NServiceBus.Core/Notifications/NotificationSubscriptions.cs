namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Notification Subscriptions.
    /// </summary>
    public class NotificationSubscriptions
    {
        internal IEnumerable<ISubscription> Get<T>()
        {
            List<ISubscription> subscribers;

            if (!subscriptions.TryGetValue(typeof(T), out subscribers))
            {
                return Enumerable.Empty<ISubscription>();
            }

            return subscribers;
        }

        /// <summary>
        /// Adds a subscription for a given event type.
        /// </summary>
        /// <param name="subscription">The callback to be invoked when the event occurs.</param>
        /// <typeparam name="T">Event type.</typeparam>
        public void Subscribe<T>(Func<T, Task> subscription)
        {
            var eventType = typeof(T);

            List<ISubscription> currentSubscriptions;

            if (!subscriptions.TryGetValue(eventType, out currentSubscriptions))
            {
                currentSubscriptions = new List<ISubscription>();
                subscriptions[eventType] = currentSubscriptions;
            }

            currentSubscriptions.Add(new Subscription<T>(subscription));
        }

        Dictionary<Type, List<ISubscription>> subscriptions = new Dictionary<Type, List<ISubscription>>();

        class Subscription<T> : ISubscription
        {
            public Subscription(Func<T, Task> invocation)
            {
                this.invocation = invocation;
            }

            public Task Invoke(object @event)
            {
                return invocation((T) @event);
            }

            Func<T, Task> invocation;
        }

        internal interface ISubscription
        {
            Task Invoke(object @event);
        }
    }
}