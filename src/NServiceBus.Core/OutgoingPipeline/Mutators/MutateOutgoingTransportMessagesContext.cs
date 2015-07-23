namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Context class for <see cref="IMutateOutgoingTransportMessages"/>.
    /// </summary>
    public class MutateOutgoingTransportMessagesContext
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessagesContext"/>.
        /// </summary>
        public MutateOutgoingTransportMessagesContext(byte[] body, Dictionary<string, string> headers)
        {
            this.headers = headers;
            Body = body;
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// Allows headers to be set.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public void SetHeader(string key, string value)
        {
            Guard.AgainstNullAndEmpty(key,"key");
            Guard.AgainstNullAndEmpty(value, "value");

            headers[key] = value;
        }

        Dictionary<string, string> headers;
    }
}