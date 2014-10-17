namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Error message event data.
    /// </summary>
    public class FailedMessage
    {
        /// <summary>
        /// Creates a new instance of <see cref="FailedMessage"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        public FailedMessage(Dictionary<string, string> headers, byte[] body, Exception exception)
        {
            Headers = headers;
            Body = body;
            Exception = exception;
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
    }
}