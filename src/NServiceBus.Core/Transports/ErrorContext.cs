namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The context for messages that has failed processing.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// The exception that caused the message to fail.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Number of times this message delivered to us by the transport.
        /// </summary>
        public int NumberOfDeliveryAttempts { get; private set; }

        /// <summary>
        /// The active transport transaction.
        /// </summary>
        public TransportTransaction TransportTransaction { get; private set; }

        /// <summary>
        /// The headers of the failed message.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// The native id of the failed message
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The original body of the failed message.
        /// </summary>
        public Stream BodyStream { get; private set; }

        /// <summary>
        /// Initializes the error context.
        /// </summary>
        public ErrorContext(string messageId, Stream bodyStream, Exception exception, Dictionary<string, string> headers, int numberOfDeliveryAttempts,TransportTransaction transportTransaction)
        {
            MessageId = messageId;
            BodyStream = bodyStream;
            Exception = exception;
            NumberOfDeliveryAttempts = numberOfDeliveryAttempts;
            TransportTransaction = transportTransaction;
            Headers = headers;
        }
    }
}