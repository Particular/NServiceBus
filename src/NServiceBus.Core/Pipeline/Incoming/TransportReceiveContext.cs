namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : IncomingContext
    {
        internal TransportReceiveContext(IncomingMessage receivedMessage, BehaviorContext parentContext)
            : base(parentContext)
        {
            var message = new TransportMessage(receivedMessage.MessageId, receivedMessage.Headers)
            {
                Body = new byte[receivedMessage.BodyStream.Length]
            };

            receivedMessage.BodyStream.Read(message.Body, 0, message.Body.Length);

            Set(message);
        }

        /// <summary>
        /// Allows context inheritance.
        /// </summary>
        protected TransportReceiveContext(BehaviorContext parentContext)
            : base(parentContext)
        {
        }


    }
}