namespace NServiceBus.Pipeline.Contexts
{
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : BehaviorContext
    {
        internal TransportReceiveContext(IncomingMessage receivedMessage, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = receivedMessage;
            Set(Message);
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; private set; }
    }
}