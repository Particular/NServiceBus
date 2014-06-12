namespace NServiceBus.Pipeline.Contexts
{
    using Unicast;
    using Unicast.Messages;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="deliveryOptions">The delivery options.</param>
        /// <param name="message">The actual message to be sent out.</param>
        public OutgoingContext(BehaviorContext parentContext, DeliveryOptions deliveryOptions, LogicalMessage message)
            : base(parentContext)
        {
            Set(deliveryOptions);
            Set(OutgoingLogicalMessageKey, message);
        }

        /// <summary>
        /// Sending options.
        /// </summary>
        public DeliveryOptions DeliveryOptions
        {
            get { return Get<DeliveryOptions>(); }
        }

        /// <summary>
        /// Outgoing logical message.
        /// </summary>
        public LogicalMessage OutgoingLogicalMessage
        {
            get { return Get<LogicalMessage>(OutgoingLogicalMessageKey); }
        }

        /// <summary>
        /// The received message, if any.
        /// </summary>
        public TransportMessage IncomingMessage
        {
            get
            {
                TransportMessage message;

                parentContext.TryGet(IncomingContext.IncomingPhysicalMessageKey, out message);

                return message;
            }
        }

        /// <summary>
        /// The message about to be sent out.
        /// </summary>
        public TransportMessage OutgoingMessage
        {
            get { return Get<TransportMessage>(); }
        }

        const string OutgoingLogicalMessageKey = "NServiceBus.OutgoingLogicalMessage";
    }
}