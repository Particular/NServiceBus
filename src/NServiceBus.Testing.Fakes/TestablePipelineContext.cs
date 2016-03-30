namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;

    /// <summary>
    /// A testable implementation of <see cref="IPipelineContext" />.
    /// </summary>
    public class TestablePipelineContext : IPipelineContext
    {
        /// <summary>
        /// Creates a new <see cref="TestableMessageHandlerContext" /> instance.
        /// </summary>
        public TestablePipelineContext(IMessageCreator messageCreator = null)
        {
            this.messageCreator = messageCreator ?? new MessageMapper();
        }

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
        public virtual Task Send(object message, SendOptions options)
        {
            sentMessages.Enqueue(new SentMessage<object>(message, options));

            return Task.FromResult(0);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        public virtual Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return Send(messageCreator.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        public virtual Task Publish(object message, PublishOptions options)
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
        public virtual Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return Publish(messageCreator.CreateInstance(messageConstructor), publishOptions);
        }

        /// <summary>
        /// the <see cref="IMessageCreator" /> instance used to create proxy implementation for message interfaces.
        /// </summary>
        protected IMessageCreator messageCreator;

        ConcurrentQueue<SentMessage<object>> sentMessages = new ConcurrentQueue<SentMessage<object>>();

        ConcurrentQueue<PublishedMessage<object>> publishedMessages = new ConcurrentQueue<PublishedMessage<object>>();
    }
}