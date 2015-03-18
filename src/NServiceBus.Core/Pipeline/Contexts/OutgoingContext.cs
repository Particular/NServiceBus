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

        internal OutgoingHeaders OutgoingHeaders
        {
            get
            {
                OutgoingHeaders existingHeaders;
                if (TryGet(out existingHeaders))
                {
                    return existingHeaders;
                }
                existingHeaders = new OutgoingHeaders();
                Set(existingHeaders);
                return existingHeaders;
            }
        }
        
           /// <summary>
        /// Allows context inheritence
        /// </summary>
        /// <param name="parentContext"></param>
        protected OutgoingContext(BehaviorContext parentContext)
            : base(parentContext)
        {
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



        const string OutgoingLogicalMessageKey = "NServiceBus.OutgoingLogicalMessage";
    }
}