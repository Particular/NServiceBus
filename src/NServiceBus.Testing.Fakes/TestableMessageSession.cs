namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    /// A testable <see cref="IMessageSession"/> implementation.
    /// </summary>
    public partial class TestableMessageSession : TestablePipelineContext, IMessageSession
    {
        /// <summary>
        /// A list of all event subscriptions made from this session.
        /// </summary>
        public virtual Subscription[] Subscriptions => subscriptions.ToArray();

        /// <summary>
        /// A list of all event subscriptions canceled from this session.
        /// </summary>
        public virtual Unsubscription[] Unsubscription => unsubscriptions.ToArray();

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public virtual Task Subscribe(Type eventType, SubscribeOptions options)
        {
            subscriptions.Enqueue(new Subscription(eventType, options));
            return Task.FromResult(0);
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public virtual Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            unsubscriptions.Enqueue(new Unsubscription(eventType, options));
            return Task.FromResult(0);
        }

        ConcurrentQueue<Subscription> subscriptions = new ConcurrentQueue<Subscription>();

        ConcurrentQueue<Unsubscription> unsubscriptions = new ConcurrentQueue<Unsubscription>();
    }
}