namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// first level retry event data.
    /// </summary>
    public struct FirstLevelRetry
    {
        Dictionary<string, string> headers;
        byte[] body;
        Exception exception;
        int retryAttempt;

        /// <summary>
        /// Creates a new instance of <see cref="FirstLevelRetry"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt</param>
        public FirstLevelRetry(Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)
        {
            Guard.AgainstNull(headers, "headers");
            Guard.AgainstNull(body, "body");
            Guard.AgainstNull(exception, "exception");
            this.headers = headers;
            this.body = body;
            this.exception = exception;
            this.retryAttempt = retryAttempt;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get { return headers; } }

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get { return body; } }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get { return exception; } }

        /// <summary>
        ///     Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get { return retryAttempt; } }
    }
}