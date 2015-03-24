namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Transports;

    /// <summary>
    /// Context containing a physical message
    /// </summary>
    public class TransportReceiveContext : IncomingContext
    {
        internal const string IncomingPhysicalMessageKey = "NServiceBus.IncomingPhysicalMessage";

        internal TransportReceiveContext(IncomingMessage receivedMessage, BehaviorContext parentContext): base(parentContext)
        {
            PhysicalMessage = new TransportMessage(receivedMessage.MessageId, receivedMessage.Headers)
            {
                Body = new byte[receivedMessage.BodyStream.Length]
            };

            receivedMessage.BodyStream.Read(PhysicalMessage.Body, 0, PhysicalMessage.Body.Length);
        }

        /// <summary>
        /// Allows context inheritence
        /// </summary>
        /// <param name="parentContext"></param>
        protected TransportReceiveContext(BehaviorContext parentContext)
            : base(parentContext)
        {
        }


        /// <summary>
        /// The received message.
        /// </summary>
        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
            private set { Set(IncomingPhysicalMessageKey, value); }
        }
    }
}