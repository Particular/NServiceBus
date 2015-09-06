namespace NServiceBus.MessageMutator
{
    using System.Collections.Generic;

    /// <summary>
    /// Context class for <see cref="IMutateIncomingMessages"/>.
    /// </summary>
    public class MutateIncomingTransportMessageContext
    {
        byte[] body;

        /// <summary>
        /// Initializes a new instance of <see cref="MutateOutgoingTransportMessageContext"/>.
        /// </summary>
        public MutateIncomingTransportMessageContext(byte[] body, Dictionary<string, string> headers)
        {
            Guard.AgainstNull("headers", headers);
            Guard.AgainstNull("body", body);
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public byte[] Body
        {
            get { return body; }
            set
            {
                Guard.AgainstNull("value",value);
                body = value;
            }
        }

        /// <summary>
        /// The current incoming headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }


    }
}