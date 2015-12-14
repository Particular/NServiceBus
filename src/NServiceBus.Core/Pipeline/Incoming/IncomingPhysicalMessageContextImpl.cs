namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class IncomingPhysicalMessageContextImpl : IncomingContextImpl, IncomingPhysicalMessageContext
    {
        public IncomingPhysicalMessageContextImpl(IncomingMessage message, BehaviorContext parentContext)
            : base(message.MessageId, message.GetReplyToAddress(), message.Headers, parentContext)
        {
            Message = message;
        }

        public IncomingMessage Message { get; }
    }
}