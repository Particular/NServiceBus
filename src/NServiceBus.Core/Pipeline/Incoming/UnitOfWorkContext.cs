namespace NServiceBus
{
    using System.Collections.Generic;
    using Persistence;
    using Pipeline;
    using Unicast.Messages;

    class UnitOfWorkContext : IncomingContext, IUnitOfWorkContext
    {
        internal UnitOfWorkContext(SynchronizedStorageSession storageSession, IIncomingLogicalMessageContext parentContext)
            : this(parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Headers, parentContext.Message.Metadata, parentContext.Message.Instance, storageSession, parentContext)
        {
            Set(storageSession);
        }

        public UnitOfWorkContext(string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, SynchronizedStorageSession storageSession, IBehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            Set(storageSession);
            Headers = headers;
            MessageMetadata = messageMetadata;
            MessageBeingHandled = messageBeingHandled;
        }

        public SynchronizedStorageSession SynchronizedStorageSession => Get<SynchronizedStorageSession>();
        public object MessageBeingHandled { get; }
        public Dictionary<string, string> Headers { get; }
        public MessageMetadata MessageMetadata { get; }
        public bool MessageHandled { get; set; }
    }
}