// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Extensibility;
    using MessageInterfaces.MessageMapper.Reflection;

    /// <summary>
    /// A testable implementation of <see cref="IPipelineContext" />.
    /// </summary>
    public partial class TestablePipelineContext : IPipelineContext
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
        /// the <see cref="IMessageCreator" /> instance used to create proxy implementation for message interfaces.
        /// </summary>
        protected IMessageCreator messageCreator;

        ConcurrentQueue<PublishedMessage<object>> publishedMessages = new ConcurrentQueue<PublishedMessage<object>>();

        ConcurrentQueue<SentMessage<object>> sentMessages = new ConcurrentQueue<SentMessage<object>>();
    }
}