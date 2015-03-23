namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The raw message coming from the transport
    /// </summary>
    public class IncomingMessage
    {
        /// <summary>
        /// The native id of the message
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The message headers
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }
        
        /// <summary>
        /// The message body
        /// </summary>
        public Stream BodyStream { get; private set; }

        /// <summary>
        /// Creates a new message
        /// </summary>
        /// <param name="messageId">Native message id</param>
        /// <param name="headers">The message headers</param>
        /// <param name="bodyStream">The message body stream</param>
        public IncomingMessage(string messageId,Dictionary<string,string> headers,Stream bodyStream)
        {
            Guard.AgainstNullAndEmpty(messageId, "messageId");
            Guard.AgainstNull(bodyStream, "bodyStream");
            Guard.AgainstNull(headers, "headers");
            MessageId = messageId;
            Headers = headers;
            BodyStream = bodyStream;
        }
    }
}