namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    class IncomingLogicalMessageContext : IncomingContext, IIncomingLogicalMessageContext
    {
        internal IncomingLogicalMessageContext(LogicalMessage logicalMessage, IIncomingPhysicalMessageContext parentContext)
            : this(logicalMessage, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Message.Headers, parentContext)
        {
        }

        public IncomingLogicalMessageContext(LogicalMessage logicalMessage, string messageId, string replyToAddress, Dictionary<string, string> headers, IBehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            Message = logicalMessage;
            Headers = headers;
            Set(logicalMessage);
        }

        public LogicalMessage Message { get; }

        public Dictionary<string, string> Headers { get; }

        public bool MessageHandled { get; set; }

        public void UpdateMessageInstance(object newInstance)
        {
            Guard.AgainstNull(nameof(newInstance), newInstance);
            var sameInstance = ReferenceEquals(Message.Instance, newInstance);

            Message.Instance = newInstance;

            if (sameInstance)
            {
                return;
            }

            var factory = Builder.Build<LogicalMessageFactory>();
            var newLogicalMessage = factory.Create(newInstance);

            Message.Metadata = newLogicalMessage.Metadata;
        }
    }
}