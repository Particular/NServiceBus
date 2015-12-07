namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Messages;

    class LogicalMessageProcessingContextImpl : IncomingContextImpl, LogicalMessageProcessingContext
    {
        internal LogicalMessageProcessingContextImpl(LogicalMessage logicalMessage, PhysicalMessageProcessingContext parentContext)
            : this(logicalMessage, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Message.Headers, parentContext)
        {
        }

        public LogicalMessageProcessingContextImpl(LogicalMessage logicalMessage, string messageId, string replyToAddress, Dictionary<string, string> headers, BehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            Message = logicalMessage;
            Headers = headers;
            Set(logicalMessage);
        }

        public LogicalMessage Message { get; private set; }

        public Dictionary<string, string> Headers { get; private set; }

        public bool MessageHandled { get; set; }
    }
}