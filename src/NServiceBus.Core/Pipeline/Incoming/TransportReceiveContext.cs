namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
    {
        /// <summary>
        /// Creates a new transport receive context.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="transportTransaction">The transport transaction.</param>
        /// <param name="parentContext">The parent context.</param>
        public TransportReceiveContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, IBehaviorContext parentContext)
            : base(parentContext)
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