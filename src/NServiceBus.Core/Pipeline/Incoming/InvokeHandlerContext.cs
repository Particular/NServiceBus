namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Persistence;
    using Pipeline;
    using Unicast.Messages;

    class InvokeHandlerContext : IncomingContext, IInvokeHandlerContext
    {
        internal InvokeHandlerContext(MessageHandler handler, SynchronizedStorageSession storageSession, IIncomingLogicalMessageContext parentContext)
            : this(handler, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Headers, parentContext.Message.Metadata, parentContext.Message.Instance, storageSession, parentContext)
        {
        }

        public InvokeHandlerContext(MessageHandler handler, string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, SynchronizedStorageSession storageSession, IBehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            MessageHandler = handler;
            Headers = headers;
            MessageBeingHandled = messageBeingHandled;
            MessageMetadata = messageMetadata;
            Set(storageSession);
        }

        public MessageHandler MessageHandler { get; }

        public SynchronizedStorageSession SynchronizedStorageSession => Get<SynchronizedStorageSession>();

        public Dictionary<string, string> Headers { get; }

        public object MessageBeingHandled { get; }

        public bool HandlerInvocationAborted { get; private set; }

        public MessageMetadata MessageMetadata { get; }

        public bool HandleCurrentMessageLaterWasCalled { get; private set; }

        public async Task HandleCurrentMessageLater()
        {
            await MessageOperationsInvokeHandlerContext.HandleCurrentMessageLater(this).ConfigureAwait(false);
            HandleCurrentMessageLaterWasCalled = true;
            DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }
    }
}