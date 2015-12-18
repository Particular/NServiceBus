namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of handling a logical message by a handler.
    /// </summary>
    public class InvokeHandlerContext : IncomingContext, IInvokeHandlerContext
    {
        internal InvokeHandlerContext(MessageHandler handler, SynchronizedStorageSession storageSession, IIncomingLogicalMessageContext parentContext)
            : this(handler, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Headers, parentContext.Message.Metadata, parentContext.Message.Instance, storageSession, parentContext)
        {
        }

        /// <summary>
        /// Creates a new instance of an invoke handler context.
        /// </summary>
        /// <param name="handler">The message handler.</param>
        /// <param name="messageId">The message id.</param>
        /// <param name="replyToAddress">The reply to address.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="messageMetadata">The message metadata.</param>
        /// <param name="messageBeingHandled">The message being handled.</param>
        /// <param name="storageSession">The storage session.</param>
        /// <param name="parentContext">The parent context.</param>
        public InvokeHandlerContext(MessageHandler handler, string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, SynchronizedStorageSession storageSession, IBehaviorContext parentContext)
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
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made 
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        public SynchronizedStorageSession SynchronizedStorageSession => Get<SynchronizedStorageSession>();

        /// <summary>
        /// Token
        /// </summary>
        public CancellationToken CancellationToken => this.Get<CancellationTokenSource>().Token;

        /// <summary>
        /// Message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        public object MessageBeingHandled { get; }

        /// <summary>
        /// <code>true</code> if <see cref="IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers" /> or <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        public bool HandlerInvocationAborted { get; private set; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; }

        /// <summary>
        /// Indicates whether <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        public bool HandleCurrentMessageLaterWasCalled { get; private set; }

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        public async Task HandleCurrentMessageLater()
        {
            await BusOperationsInvokeHandlerContext.HandleCurrentMessageLater(this).ConfigureAwait(false);
            HandleCurrentMessageLaterWasCalled = true;
            DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        /// <summary>
        /// Tells the bus to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }
    }
}