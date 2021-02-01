namespace NServiceBus
{
    using Pipeline;
    using Transport;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
    {
        /// <summary>
        /// Creates a new transport receive context.
        /// </summary>
        public TransportReceiveContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, RootContext rootContext)
            : base(rootContext)
        {
            Message = receivedMessage;
            Set(Message);
            Set(transportTransaction);
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; }

        /// <summary>
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// </summary>
        public void AbortReceiveOperation()
        {
            ReceiveOperationWasAborted = true;
        }

        internal bool ReceiveOperationWasAborted { get; private set; }

    }
}