namespace NServiceBus.Transport
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
        /// Exception that caused the message processing to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Transport transaction for failed receive message.
        /// </summary>
        public TransportTransaction TransportTransaction { get; }

        /// <summary>
        /// Number of immediate processing attempts.
        /// </summary>
        public int NumberOfDeliveryAttempts { get; }

        /// <summary>
        /// Failed incoming message.
        /// </summary>
        public IncomingMessage Message { get; }

        /// <summary>
        /// Initializes the error context.
        /// </summary>
        public ErrorContext(Exception exception, Dictionary<string, string> headers, string transportMessageId, Stream bodyStream, TransportTransaction transportTransaction, int numberOfDeliveryAttempts)
        {
            Exception = exception;
            TransportTransaction = transportTransaction;
            NumberOfDeliveryAttempts = numberOfDeliveryAttempts;

            Message = new IncomingMessage(transportMessageId, headers, bodyStream);

            //Incoming message reads the body stream so we need to rewind it
            Message.BodyStream.Position = 0;
        }
    }
}