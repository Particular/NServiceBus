namespace NServiceBus
{
    using System;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class SendOptions : SendLocalOptions
    {
        internal MessageIntentEnum Intent = MessageIntentEnum.Send;

        /// <summary>
        ///     Creates an instance of <see cref="SendOptions" />.
        /// </summary>
        /// <param name="destination">Specifies a specific destination for the message.</param>
        /// <param name="correlationId">Specifies a custom currelation id for the message.</param>
        /// <param name="deliverAt">Tells the bus to deliver the message at the given time.</param>
        /// <param name="delayDeliveryFor">Tells the bus to wait the specified amount of time before delivering the message.</param>
        public SendOptions(string destination = null, string correlationId = null, DateTime? deliverAt = null, TimeSpan? delayDeliveryFor = null):
            base(correlationId, deliverAt, delayDeliveryFor)
        {
            Destination = destination;
        }

        /// <summary>
        ///     Adds a header for the message to be send.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public new SendOptions AddHeader(string key, string value)
        {
            Headers[key] = value;
            return this;
        }

        internal string Destination { get; private set; }

        /// <summary>
        ///     Sets a custom message id for this message.
        /// </summary>
        /// <param name="messageId"></param>
        public new SendOptions SetCustomMessageId(string messageId)
        {
            MessageId = messageId;
            return this;
        }

        internal void AsReplyTo(string replyToAddress)
        {
            Guard.AgainstNull(replyToAddress, "replyToAddress");

            Destination = replyToAddress;
            Intent = MessageIntentEnum.Reply;
        }
    }
}