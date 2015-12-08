namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// first level retry event data.
    /// </summary>
    public struct FirstLevelRetry
    {
        /// <summary>
        /// Creates a new instance of <see cref="FirstLevelRetry"/>.
        /// </summary>
        /// <param name="messageId">The id of the failed message.</param>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        public FirstLevelRetry(string messageId,Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)
        {
            Guard.AgainstNullAndEmpty("messageId", messageId);
            Guard.AgainstNull("headers", headers);
            Guard.AgainstNull("body", body);
            Guard.AgainstNull("exception", exception);

            MessageId = messageId;
            Headers = headers;
            Body = body;
            Exception = exception;
            RetryAttempt = retryAttempt;
        }

        /// <summary>
        /// Id of the failed message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        ///     Gets a byte array to the body content of the message.
        /// </summary>
        public byte[] Body { get; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; }
    }
}