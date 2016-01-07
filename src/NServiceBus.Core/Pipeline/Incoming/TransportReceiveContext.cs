namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
    {
        public TransportReceiveContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Message = receivedMessage;
            Set(Message);
            Set(transportTransaction);
        }

        public IncomingMessage Message { get; }
    }
}