namespace NServiceBus.Pipeline.Contexts
{
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : IncomingContext
    {
        internal TransportReceiveContext(IncomingMessage receivedMessage, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = new TransportMessage(receivedMessage.MessageId, receivedMessage.Headers)
            {
                Body = new byte[receivedMessage.BodyStream.Length]
            };

            receivedMessage.BodyStream.Read(Message.Body, 0, Message.Body.Length);

            Set(Message);
        }

        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        public TransportMessage Message { get; }
    }
}