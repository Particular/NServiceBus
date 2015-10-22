namespace NServiceBus.Pipeline.Contexts
{
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : BehaviorContext
    {
        internal TransportReceiveContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = receivedMessage;
            Set(Message);
            Set(transportTransaction);
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; private set; }
    }
}