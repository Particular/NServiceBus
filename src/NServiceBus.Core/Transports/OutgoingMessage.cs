namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The message going out to the transport
    /// </summary>
    public class OutgoingMessage
    {
        /// <summary>
        /// Constructs the message
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="headers">The headers associated with this message</param>
        /// <param name="body">The body of the message</param>
        public OutgoingMessage(string messageId, Dictionary<string, string> headers, byte[] body)
        {
            MessageId = messageId;
            Headers = headers;
            Body = body;

            Headers[NServiceBus.Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;
            Headers[NServiceBus.Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }

        /// <summary>
        /// The body to be sent
        /// </summary>
        public byte[] Body { get; private set; }


        /// <summary>
        /// The id of the message
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The headers for the message
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }
    }
}