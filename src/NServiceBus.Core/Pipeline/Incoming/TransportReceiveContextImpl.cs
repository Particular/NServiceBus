namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Transports;

    class TransportReceiveContextImpl : BehaviorContextImpl, TransportReceiveContext
    {
        internal TransportReceiveContextImpl(IncomingMessage receivedMessage, TransportTransaction transportTransaction, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = receivedMessage;
            Set(Message);
            Set(transportTransaction);
        }

        public IncomingMessage Message { get; }
    }
}