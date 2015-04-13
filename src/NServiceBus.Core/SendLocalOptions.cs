namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class SendLocalOptions
    {
        readonly DateTime? at;
        readonly string correlationId;
        readonly TimeSpan? delay;
        internal Dictionary<string, object> Context = new Dictionary<string, object>();
        internal Dictionary<string, string> Headers = new Dictionary<string, string>();
        internal string MessageId;

        /// <summary>
        ///     Creates an instance of <see cref="SendOptions" />.
        /// </summary>
        /// <param name="correlationId">Specifies a custom currelation id for the message.</param>
        /// <param name="deliverAt">Tells the bus to deliver the message at the given time.</param>
        /// <param name="delayDeliveryFor">Tells the bus to wait the specified amount of time before delivering the message.</param>
        public SendLocalOptions(string correlationId = null, DateTime? deliverAt = null, TimeSpan? delayDeliveryFor = null)
        {
            if (deliverAt != null && delayDeliveryFor != null)
            {
                throw new ArgumentException("Ensure you either set `deliverAt` or `delayDeliveryFor`, but not both.");
            }

            delay = delayDeliveryFor;
            at = deliverAt;
            this.correlationId = correlationId;
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

        /// <summary>
        ///     Adds a header for the message to be send.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public SendLocalOptions AddHeader(string key, string value)
        {
            Headers[key] = value;
            return this;
        }

        /// <summary>
        ///     Sets a custom message id for this message.
        /// </summary>
        /// <param name="messageId"></param>
        public SendLocalOptions SetCustomMessageId(string messageId)
        {
            MessageId = messageId;
            return this;
        }
    }
}