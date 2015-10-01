namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// first level retry event data.
    /// </summary>
    public struct FirstLevelRetry
    {
        /// <summary>
        /// Creates a new instance of <see cref="FirstLevelRetry"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "FirstLevelRetry(Dictionary<string, string> headers, Stream body, Exception exception, int retryAttempt)")]
        public FirstLevelRetry(Dictionary<string, string> headers, byte[] body, Exception exception, int retryAttempt)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new instance of <see cref="FirstLevelRetry"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        /// <param name="retryAttempt">Number of retry attempt.</param>
        public FirstLevelRetry(Dictionary<string, string> headers, Stream body, Exception exception, int retryAttempt)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(body), body);
            Guard.AgainstNull(nameof(exception), exception);
            Guard.AgainstNull(nameof(body), body);

            Headers = headers;
            BodyStream = body;
            Exception = exception;
            RetryAttempt = retryAttempt;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        ///     Gets a byte array to the body content of the message.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "BodyStream")]
        public byte[] Body
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        ///     Getsthe body content of the message.
        /// </summary>
        public Stream BodyStream { get; }

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