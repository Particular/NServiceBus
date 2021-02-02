namespace NServiceBus
{
    using Pipeline;
    using Transport;

    class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
    {
        public TransportReceiveContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, RootContext rootContext)
            : base(rootContext)
        {
            Message = receivedMessage;
            Set(Message);
            Set(transportTransaction);
        }

        public IncomingMessage Message { get; }

        public void AbortReceiveOperation()
        {
            ReceiveOperationWasAborted = true;
        }

        public bool ReceiveOperationWasAborted { get; private set; }
    }
}