namespace NServiceBus.Faults
{
    using System.Collections.Generic;

    /// <summary>
    /// Error message event data.
    /// </summary>
    public struct FailedMessage
    {
        /// <summary>
        /// Creates a new instance of <see cref="FailedMessage" />.
        /// </summary>
        /// <param name="messageId">The id of the failed message.</param>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exceptionInfo">Exception thrown.</param>
        public FailedMessage(string messageId, Dictionary<string, string> headers, byte[] body, ExceptionInfo exceptionInfo)
        {
            MessageId = messageId;
            Headers = headers;
            Body = body;
            ExceptionInfo = exceptionInfo;
        }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets a byte array to the body content of the message.
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        /// The exception that caused this message to fail.
        /// </summary>
        public ExceptionInfo ExceptionInfo { get; }

        /// <summary>
        /// The id of the failed message.
        /// </summary>
        public string MessageId { get; }
    }
}