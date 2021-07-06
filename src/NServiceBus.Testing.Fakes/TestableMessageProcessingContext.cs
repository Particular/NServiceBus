namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Testable implementation of <see cref="IMessageProcessingContext" />.
    /// </summary>
    public partial class TestableMessageProcessingContext : TestablePipelineContext, IMessageProcessingContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableMessageProcessingContext" />.
        /// </summary>
        public TestableMessageProcessingContext(IMessageCreator messageCreator = null) : base(messageCreator)
        {
        }

        /// <summary>
        /// A list of all messages sent by <see cref="IMessageProcessingContext.Reply" />.
        /// </summary>
        public virtual RepliedMessage<object>[] RepliedMessages => repliedMessages.ToArray();

        /// <summary>
        /// A list of all forwarding destinations set by <see cref="IMessageProcessingContext.ForwardCurrentMessageTo" />.
        /// </summary>
        public virtual string[] ForwardedMessages => forwardedMessages.ToArray();

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message.
        /// </summary>
        public IDictionary<string, string> MessageHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        public virtual Task Reply(object message, ReplyOptions options)
        {
            repliedMessages.Enqueue(new RepliedMessage<object>(message, options));
            return Task.FromResult(0);
        }

        /// <summary>
        /// Instantiates a message of type T and performs a regular
        /// <see cref="M:NServiceBus.IMessageProcessingContext.Reply(System.Object,NServiceBus.ReplyOptions)" />.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        public virtual Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return Reply(messageCreator.CreateInstance(messageConstructor), options);
        }

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        public virtual Task ForwardCurrentMessageTo(string destination)
        {
            forwardedMessages.Enqueue(destination);
            return Task.FromResult(0);
        }

        /// <summary>
        /// The Id of the currently processed message.
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The address of the endpoint that sent the current message being handled.
        /// </summary>
        public string ReplyToAddress { get; set; } = "reply address";

        ConcurrentQueue<string> forwardedMessages = new ConcurrentQueue<string>();

        ConcurrentQueue<RepliedMessage<object>> repliedMessages = new ConcurrentQueue<RepliedMessage<object>>();
    }
}