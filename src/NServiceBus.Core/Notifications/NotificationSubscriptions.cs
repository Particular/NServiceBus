namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    ///
    /// </summary>
    public class NotificationSubscriptions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<ISubscription> Get<T>()
        {
            List<ISubscription> subscribers;

            if (!subscriptions.TryGetValue(typeof(T), out subscribers))
            {
                return Enumerable.Empty<ISubscription>();
            }

            return subscribers;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subscription"></param>
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

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Subscription<T> : ISubscription
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="invocation"></param>
            public Subscription(Func<T, Task> invocation)
            {
                this.invocation = invocation;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="event"></param>
            /// <returns></returns>
            public Task Invoke(object @event)
            {
                return invocation((T) @event);
            }

            Func<T, Task> invocation;
        }

        /// <summary>
        ///
        /// </summary>
        public interface ISubscription
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="event"></param>
            /// <returns></returns>
            Task Invoke(object @event);
        }
    }
}