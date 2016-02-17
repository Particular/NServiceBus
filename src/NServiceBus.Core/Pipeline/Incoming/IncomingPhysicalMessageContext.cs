namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class IncomingPhysicalMessageContext : IncomingContext, IIncomingPhysicalMessageContext
    {
        public IncomingPhysicalMessageContext(IncomingMessage message, IBehaviorContext parentContext)
            : base(message.MessageId, message.GetReplyToAddress(), message.Headers, parentContext)
        {
            Message = message;
        }

        public IncomingMessage Message { get; }

        public void UpdateMessage(byte[] body)
        {
            Message.Body = body;
        }
    }
}