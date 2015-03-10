namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
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
        /// <param name="headers">The headers fór the message</param>
        public OutgoingContext(BehaviorContext parentContext, DeliveryOptions deliveryOptions, LogicalMessage message,Dictionary<string,string> headers)
            : base(parentContext)
        {
            Headers = headers;
            Set(deliveryOptions);
            Set(OutgoingLogicalMessageKey, message);
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

        /// <summary>
        ///     Gets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }


        const string OutgoingLogicalMessageKey = "NServiceBus.OutgoingLogicalMessage";

        /// <summary>
        /// Tells if this outgoing message is a control message
        /// </summary>
        /// <returns></returns>
        public bool IsControlMessage()
        {
            return Headers.ContainsKey(NServiceBus.Headers.ControlMessageHeader);
        }
    }
}