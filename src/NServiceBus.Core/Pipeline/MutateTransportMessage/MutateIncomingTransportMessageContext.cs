namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Context class for <see cref="IMutateIncomingTransportMessages"/>.
    /// </summary>
    public class MutateIncomingTransportMessageContext
    {
        Stream body;

        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext"/>.
        /// </summary>
        public MutateIncomingTransportMessageContext(Stream body, IDictionary<string, string> headers)
        {
            Guard.AgainstNull("headers", headers);
            Guard.AgainstNull("body", body);
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public Stream Body
        {
            get { return body; }
            set
            {
                Guard.AgainstNull("value", value);
                body = value;
            }
        }

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; private set; }

    }
}