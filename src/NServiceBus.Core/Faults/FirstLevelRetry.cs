namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// first level retry event data.
    /// </summary>
    public class FirstLevelRetry
    {
        /// <summary>
        /// Creates a new instance of <see cref="FirstLevelRetry"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt</param>
        public FirstLevelRetry(Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)
        {
            Headers = headers;
            Body = body;
            Exception = exception;
            RetryAttempt = retryAttempt;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get; private set; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        ///     Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; private set; }
    }
}