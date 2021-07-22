namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Delayed Retry event data.
    /// </summary>
    public class DelayedRetryMessage
    {
        /// <summary>
        /// Creates a new instance of <see cref="DelayedRetryMessage" />.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        public DelayedRetryMessage(string messageId, Dictionary<string, string> headers, MessageBody body, Exception exception, int retryAttempt)
        {
            Headers = headers;
            Body = body;
            Exception = exception;
            RetryAttempt = retryAttempt;
            MessageId = messageId;
        }

        /// <summary>
        /// Id of the failed message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets a byte array to the body content of the message.
        /// </summary>
        public MessageBody Body { get; }

        /// <summary>
        /// The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; }
    }
}