namespace NServiceBus
{
    using Pipeline;
    using Transport;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    partial class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
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
    }
}