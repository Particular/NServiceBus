namespace NServiceBus.Faults
{
    using System.Collections.Generic;

    /// <summary>
    /// first level retry event data.
    /// </summary>
    public struct FirstLevelRetry
    {
        /// <summary>
        /// Creates a new instance of <see cref="FirstLevelRetry" />.
        /// </summary>
        /// <param name="messageId">The id of the failed message.</param>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exceptionInfo">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        public FirstLevelRetry(string messageId, Dictionary<string, string> headers, byte[] body, ExceptionInfo exceptionInfo, int retryAttempt)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(body), body);
            Guard.AgainstNull(nameof(exceptionInfo), exceptionInfo);

            MessageId = messageId;
            Headers = headers;
            Body = body;
            ExceptionInfo = exceptionInfo;
            RetryAttempt = retryAttempt;
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
        public ExceptionInfo ExceptionInfo { get; }

        /// <summary>
        /// Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; }
    }
}