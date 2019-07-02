namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

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
        public DelayedRetryMessage(string messageId, Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)
        {
            Headers = headers;
            Body = body;
            Exception = exception;
            RetryAttempt = retryAttempt;
            MessageId = messageId;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DelayedRetryMessage" />.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        [ObsoleteEx(RemoveInVersion = "8.0", ReplacementTypeOrMember = "DelayedRetryMessage(string messageId, Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)")]
        public DelayedRetryMessage(Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)
        {
            Headers = headers;
            Body = body;
            Exception = exception;
            RetryAttempt = retryAttempt;
            MessageId = headers[NServiceBus.Headers.MessageId]; // safe because IncomingMessage has already set that header
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
        public byte[] Body { get; }

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