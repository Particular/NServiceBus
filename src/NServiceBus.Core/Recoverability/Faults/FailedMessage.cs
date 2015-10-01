namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Error message event data.
    /// </summary>
    public struct FailedMessage
    {
        /// <summary>
        /// Creates a new instance of <see cref="FailedMessage"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "FailedMessage(Dictionary<string, string> headers, Stream body, Exception exception)")]
        public FailedMessage(Dictionary<string, string> headers, byte[] body, Exception exception)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new instance of <see cref="FailedMessage"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        /// <param name="body">Message body.</param>
        /// <param name="exception">Exception thrown.</param>
        public FailedMessage(Dictionary<string, string> headers, Stream body, Exception exception)
        {
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(body), body);
            Guard.AgainstNull(nameof(exception), exception);

            Headers = headers;
            BodyStream = body;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        ///     Gets a byte array to the body content of the message.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer used")]
        public byte[] Body
        {
            get { throw new NotImplementedException();}
        }

        /// <summary>
        ///     Gets the body content of the message.
        /// </summary>
        public Stream BodyStream { get; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; }
    }
}