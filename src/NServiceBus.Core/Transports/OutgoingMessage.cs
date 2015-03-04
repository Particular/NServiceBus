namespace NServiceBus.Transports
{
    using System.Collections.Generic;

    /// <summary>
    /// The message going out to the transport
    /// </summary>
    public class OutgoingMessage
    {
        /// <summary>
        /// Constructs the message
        /// </summary>
        /// <param name="headers">The headers associated with this message</param>
        /// <param name="body">The body of the message</param>
        public OutgoingMessage(Dictionary<string, string> headers,byte[] body)
        {
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// The body to be sent
        /// </summary>
        public byte[] Body { get; private set; }


        /// <summary>
        /// The headers for the message
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }
    }
}