namespace NServiceBus
{
    using System;
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

        // remove in v8
        public bool HandleCurrentMessageLaterWasCalled => throw new NotSupportedException();

        // remove in v8
        public Task HandleCurrentMessageLater()
        {
            throw new NotImplementedException();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }
    }
}