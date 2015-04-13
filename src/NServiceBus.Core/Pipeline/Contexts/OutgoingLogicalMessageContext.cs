namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="deliveryMessageOptions">The delivery options.</param>
        /// <param name="message">The actual message to be sent out.</param>
        /// <param name="headers">The headers fór the message</param>
        /// <param name="messageId">The id of the message</param>
        /// <param name="intent">The intent of the message</param>
        public OutgoingContext(BehaviorContext parentContext, DeliveryMessageOptions deliveryMessageOptions, LogicalMessage message, Dictionary<string, string> headers, string messageId, MessageIntentEnum intent)
            : base(parentContext)
        {
            DeliveryMessageOptions = deliveryMessageOptions;
            OutgoingLogicalMessage = message;
            Headers = headers;
            MessageId = messageId;
            Intent = intent;
        }

        /// <summary>
        /// Sending options.
        /// </summary>
        public DeliveryMessageOptions DeliveryMessageOptions { get; private set; }

        /// <summary>
        /// Outgoing logical message.
        /// </summary>
        public LogicalMessage OutgoingLogicalMessage { get; private set; }

        /// <summary>
        ///     Gets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// This id of this message
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The intent of the message
        /// </summary>
        public MessageIntentEnum Intent { get; private set; }

        /// <summary>
        /// Tells if this outgoing message is a control message
        /// </summary>
        /// <returns></returns>
        public bool IsControlMessage()
        {
            return OutgoingLogicalMessage is ControlMessage;
        }
    }
}