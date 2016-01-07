namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Messages;

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
    }
}