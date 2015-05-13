namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class SendLocalOptions:ExtendableOptions
    {
        readonly DateTime? at;
        readonly string correlationId;
        readonly TimeSpan? delay;
        internal Dictionary<string, object> Context = new Dictionary<string, object>();
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