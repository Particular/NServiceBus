namespace NServiceBus.Faults
{
    using System;

    /// <summary>
    /// Error message event data.
    /// </summary>
    public class FailedMessage
    {
        /// <summary>
        /// Creates a new instance of <see cref="FailedMessage" />.
        /// </summary>
        /// <param name="messageId">The id of the failed message.</param>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="errorQueue">Error queue address.</param>
        public FailedMessage(string messageId, HeaderDictionary headers, byte[] body, Exception exception, string errorQueue)
        {
            MessageId = messageId;
            Headers = headers;
            Body = body;
            Exception = exception;
            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        public HeaderDictionary Headers { get; }

        /// <summary>
        /// Gets a byte array to the body content of the message.
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        /// The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Error queue address to which failed message has been moved.
        /// </summary>
        public string ErrorQueue { get; }

        /// <summary>
        /// The id of the failed message.
        /// </summary>
        public string MessageId { get; }
    }
}