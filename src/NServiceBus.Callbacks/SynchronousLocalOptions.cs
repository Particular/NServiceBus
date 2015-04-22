namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class SynchronousLocalOptions
    {
        readonly string correlationId;
        internal Dictionary<string, string> Headers = new Dictionary<string, string>();
        internal string MessageId;

        /// <summary>
        ///     Creates an instance of <see cref="NServiceBus.SynchronousLocalOptions" />.
        /// </summary>
        /// <param name="correlationId">Specifies a custom currelation id for the message.</param>
        public SynchronousLocalOptions(string correlationId = null)
        {

            this.correlationId = correlationId;
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
        public SynchronousLocalOptions AddHeader(string key, string value)
        {
            Headers[key] = value;
            return this;
        }

        /// <summary>
        ///     Sets a custom message id for this message.
        /// </summary>
        /// <param name="messageId"></param>
        public SynchronousLocalOptions SetCustomMessageId(string messageId)
        {
            MessageId = messageId;
            return this;
        }
    }
}