namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class SendOptions
    {
        readonly DateTime? at;
        readonly string correlationId;
        readonly TimeSpan? delay;
        
        /// <summary>
        ///     Creates an instance of <see cref="SendOptions" />.
        /// </summary>
        /// <param name="destination">Specifies a specific destination for the message.</param>
        /// <param name="correlationId">Specifies a custom currelation id for the message.</param>
        /// <param name="deliverAt">Tells the bus to deliver the message at the given time.</param>
        /// <param name="delayDeliveryFor">Tells the bus to wait the specified amount of time before delivering the message.</param>
        public SendOptions(string destination = null, string correlationId = null, DateTime? deliverAt = null, TimeSpan? delayDeliveryFor = null)
        {
            if (deliverAt != null && delayDeliveryFor != null)
            {
                throw new ArgumentException("Ensure you either set `deliverAt` or `delayDeliveryFor`, but not both.");
            }

            Destination = destination;
            delay = delayDeliveryFor;
            at = deliverAt;
            this.correlationId = correlationId;
        }
        
        /// <summary>
        ///     Adds a header for the message to be send.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }

        /// <summary>
        ///     Sets the destination of the message to the local endpoint.
        /// </summary>
        public void SetLocalEndpointAsDestination()
        {
            SendToLocalEndpoint = true;
            Destination = null;
        }

        /// <summary>
        ///     Sets a custom message id for this message.
        /// </summary>
        /// <param name="messageId"></param>
        public void SetCustomMessageId(string messageId)
        {
            MessageId = messageId;
        }

        internal TimeSpan? Delay
        {
            get { return delay; }
        }

        internal DateTime? At
        {
            get { return at; }
        }

        internal string CorrelationId
        {
            get { return correlationId; }
        }

        internal string Destination { get; private set; }

        internal void AsReplyTo(string replyToAddress)
        {
            Guard.AgainstNull(replyToAddress, "replyToAddress");

            Destination = replyToAddress;
            Intent = MessageIntentEnum.Reply;
        }

        internal Dictionary<string, string> Headers = new Dictionary<string, string>();
        internal MessageIntentEnum Intent = MessageIntentEnum.Send;
        internal string MessageId;
        internal bool SendToLocalEndpoint;
    }
}