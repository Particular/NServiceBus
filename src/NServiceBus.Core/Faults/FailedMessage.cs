namespace NServiceBus.Faults
{
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
        /// <param name="exception">Exception data.</param>
        public FailedMessage(Dictionary<string, string> headers, byte[] body, FailedMessageException exception)
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
        /// The exception data.
        /// </summary>
        public FailedMessageException Exception { get; private set; }

        /// <summary>
        /// A failed message exception data.
        /// </summary>
        public class FailedMessageException
        {
            /// <summary>
            /// Creates a new instance of <see cref="FailedMessageException"/>.
            /// </summary>
            /// <param name="type">The exception type.</param>
            /// <param name="message">The exception message.</param>
            /// <param name="source">The exception source.</param>
            /// <param name="stacktrace">The exception stacktrace or ToString representation.</param>
            public FailedMessageException(string type, string message, string source, string stacktrace)
            {
                Type = type;
                Message = message;
                Source = source;
                StackTrace = stacktrace;
            }
            /// <summary>
            ///     The exception type.
            /// </summary>
            public string Type { get; private set; }

            /// <summary>
            /// Gets a message that describes the current exception.
            /// </summary>
            public string Message { get; private set; }

            /// <summary>
            /// Gets the name of the application or the object that causes the error.
            /// </summary>
            public string Source { get; private set; }

            /// <summary>
            /// The exception stacktrace or a ToString representation of the exception.
            /// </summary>
            public string StackTrace { get; private set; }
        }
    }

    
}