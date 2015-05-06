namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class RequestResponseOptions
    {
        internal Dictionary<string, string> Headers = new Dictionary<string, string>();
        readonly string correlationId;
        internal string MessageId;
        internal CancellationToken CancellationToken;

        /// <summary>
        ///     Creates an instance of <see cref="RequestResponseOptions" />.
        /// </summary>
        /// <param name="destination">Specifies a specific destination for the message.</param>
        /// <param name="correlationId">Specifies a custom currelation id for the message.</param>
        /// <param name="cancellationToken">The cancellation token which allows to cancel the callback</param>
        public RequestResponseOptions(string destination = null, string correlationId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.correlationId = correlationId;            
            Destination = destination;

            if (cancellationToken == default(CancellationToken))
            {
                cancellationToken = CancellationToken.None;
            }

            CancellationToken = cancellationToken;
        }

        internal string Destination { get; private set; }

        internal string CorrelationId
        {
            get { return correlationId; }
        }

        /// <summary>
        ///     Adds a header for the message to be send.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public RequestResponseOptions AddHeader(string key, string value)
        {
            Headers[key] = value;
            return this;
        }

        /// <summary>
        ///     Sets a custom message id for this message.
        /// </summary>
        /// <param name="messageId"></param>
        public RequestResponseOptions SetCustomMessageId(string messageId)
        {
            MessageId = messageId;
            return this;
        }
    }
}