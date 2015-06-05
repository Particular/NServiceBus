namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Context class for IMutateOutgoingPhysicalContext
    /// </summary>
    public class MutateOutgoingPhysicalMessageContext
    {

        /// <summary>
        /// Ctor
        /// </summary>
        public MutateOutgoingPhysicalMessageContext(byte[] body, Dictionary<string, string> headers)
        {
            this.headers = headers;
            Body = body;
        }

        /// <summary>
        /// The body of the message
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// Allows headers to be set
        /// </summary>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public void SetHeader(string key, string value)
        {
            headers[key] = value;
        }

        readonly Dictionary<string, string> headers;
    }
}