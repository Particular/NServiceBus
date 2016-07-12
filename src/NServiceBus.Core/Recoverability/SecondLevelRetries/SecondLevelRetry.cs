namespace NServiceBus.Faults
{
    using System.Collections.Generic;

    /// <summary>
    /// Second level retry event data.
    /// </summary>
    public struct SecondLevelRetry
    {
        /// <summary>
        /// Creates a new instance of <see cref="SecondLevelRetry" />.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exceptionInfo">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        public SecondLevelRetry(Dictionary<string, string> headers, byte[] body, ExceptionInfo exceptionInfo, int retryAttempt)
        {
            Headers = headers;
            Body = body;
            ExceptionInfo = exceptionInfo;
            RetryAttempt = retryAttempt;
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
        /// Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; }
    }
}