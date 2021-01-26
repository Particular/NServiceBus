namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;

    /// <summary>
    /// A testable <see cref="IMessageSession"/> implementation.
    /// </summary>
    public partial class TestableMessageSession : IMessageSession
    {
        /// <summary>
        /// Creates a new <see cref="TestableMessageHandlerContext" /> instance.
        /// </summary>
        public TestableMessageSession(IMessageCreator messageCreator = null)
        {
            this.messageCreator = messageCreator ?? new MessageMapper();
        }

        /// <summary>
        /// A list of all messages sent with a saga timeout header.
        /// </summary>
        public TimeoutMessage<object>[] TimeoutMessages => timeoutMessages.ToArray();

        /// <summary>
        /// A list of all messages sent by <see cref="IPipelineContext.Send" />.
        /// </summary>
        public virtual SentMessage<object>[] SentMessages => sentMessages.ToArray();

        /// <summary>
        /// A list of all messages published by <see cref="IPipelineContext.Publish" />,
        /// </summary>
        public virtual PublishedMessage<object>[] PublishedMessages => publishedMessages.ToArray();

        /// <summary>
        /// A <see cref="T:NServiceBus.Extensibility.ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; set; } = new ContextBag();

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        public virtual Task Send(object message, SendOptions options, CancellationToken cancellationToken = default)
        {
            var headers = options.GetHeaders();

            if (headers.ContainsKey(Headers.IsSagaTimeoutMessage))
            {
                timeoutMessages.Enqueue(GetTimeoutMessage(message, options));
            }

            sentMessages.Enqueue(new SentMessage<object>(message, options));
            return Task.FromResult(0);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        public virtual Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default)
        {
            return Send(messageCreator.CreateInstance(messageConstructor), options, cancellationToken);
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        public virtual Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default)
        {
            publishedMessages.Enqueue(new PublishedMessage<object>(message, options));
            return Task.FromResult(0);
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        public virtual Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            return Publish(messageCreator.CreateInstance(messageConstructor), publishOptions, cancellationToken);
        }

        static TimeoutMessage<object> GetTimeoutMessage(object message, SendOptions options)
        {
            var within = options.GetDeliveryDelay();
            if (within.HasValue)
            {
                return new TimeoutMessage<object>(message, options, within.Value);
            }

            var dateTimeOffset = options.GetDeliveryDate();
            return new TimeoutMessage<object>(message, options, dateTimeOffset.Value);
        }

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
        public virtual Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default)
        {
            subscriptions.Enqueue(new Subscription(eventType, options));
            return Task.FromResult(0);
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public virtual Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default)
        {
            unsubscriptions.Enqueue(new Unsubscription(eventType, options));
            return Task.FromResult(0);
        }

        /// <summary>
        /// the <see cref="IMessageCreator" /> instance used to create proxy implementation for message interfaces.
        /// </summary>
        protected IMessageCreator messageCreator;

        ConcurrentQueue<PublishedMessage<object>> publishedMessages = new ConcurrentQueue<PublishedMessage<object>>();

        ConcurrentQueue<SentMessage<object>> sentMessages = new ConcurrentQueue<SentMessage<object>>();
        ConcurrentQueue<TimeoutMessage<object>> timeoutMessages = new ConcurrentQueue<TimeoutMessage<object>>();

        ConcurrentQueue<Subscription> subscriptions = new ConcurrentQueue<Subscription>();

        ConcurrentQueue<Unsubscription> unsubscriptions = new ConcurrentQueue<Unsubscription>();
    }
}