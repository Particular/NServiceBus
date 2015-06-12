namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Error message event data.
    /// </summary>
    public struct FailedMessage
    {
        Dictionary<string, string> headers;
        byte[] body;
        Exception exception;

        /// <summary>
        /// Creates a new instance of <see cref="FailedMessage"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        public FailedMessage(Dictionary<string, string> headers, byte[] body, Exception exception)
        {
            this.headers = headers;
            this.body = body;
            this.exception = exception;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get { return headers; }}

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get { return body; } }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get { return exception; } }
    }
}