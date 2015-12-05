namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of handling a logical message by a handler.
    /// </summary>
    public class InvokeHandlerContext : IncomingContext, IMessageHandlerContext
    {
        /// <summary>
        /// Initializes the handling stage context. This is the constructor to use for internal usage.
        /// </summary>
        internal InvokeHandlerContext(MessageHandler handler, SynchronizedStorageSession storageSession, LogicalMessageProcessingContext parentContext)
            : this(handler, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Headers, parentContext.Message.Metadata, parentContext.Message.Instance, storageSession, parentContext)
        {
        }

        /// <summary>
        /// Initializes the handling stage context.
        /// </summary>
        public InvokeHandlerContext(MessageHandler handler, string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, SynchronizedStorageSession storageSession, BehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            MessageHandler = handler;
            Headers = headers;
            MessageBeingHandled = messageBeingHandled;
            MessageMetadata = messageMetadata;
            Set(storageSession);
        }

        /// <summary>
        /// The current <see cref="IHandleMessages{T}" /> being executed.
        /// </summary>
        public MessageHandler MessageHandler { get; }

        /// <summary>
        /// The transactional storage session that the handler can use to persist information in sync with receiving a message.
        /// </summary>
        public SynchronizedStorageSession SynchronizedStorageSession => Get<SynchronizedStorageSession>();

        /// <summary>
        /// Message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        public object MessageBeingHandled { get; }

        /// <summary>
        /// <code>true</code> if <see cref="DoNotContinueDispatchingCurrentMessageToHandlers" />  has been called.
        /// </summary>
        public bool HandlerInvocationAborted { get; private set; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; }

        /// <inheritdoc />
        public Task HandleCurrentMessageLater()
        {
            return BusOperationsInvokeHandlerContext.HandleCurrentMessageLater(this);
        }

        /// <inheritdoc />
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }
    }
}