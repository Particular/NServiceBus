namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Transport;

    /// <summary>
    /// The context for messages that has failed processing.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// Details of the exception that caused the message processing to fail.
        /// </summary>
        public ExceptionInfo ExceptionInfo { get; }

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
        /// Creates <see cref="ErrorContext"/>.
        /// </summary>
        public ErrorContext(ExceptionInfo exceptionInfo, Dictionary<string, string> headers, string transportMessageId, Stream bodyStream, TransportTransaction transportTransaction, int numberOfDeliveryAttempts)
        {
            ExceptionInfo = exceptionInfo;
            TransportTransaction = transportTransaction;
            NumberOfDeliveryAttempts = numberOfDeliveryAttempts;

            Message = new IncomingMessage(transportMessageId, headers, bodyStream);

            //Incoming message reads the body stream so we need to rewind it
            Message.BodyStream.Position = 0;
        }

        /// <summary>
        /// Creates <see cref="ErrorContext"/>.
        /// </summary>
        public ErrorContext(Exception exception, Dictionary<string, string> headers, string transportMessageId, Stream bodyStream, TransportTransaction transportTransaction, int numberOfDeliveryAttempts)
            : this(ExceptionInfo.FromException(exception), headers, transportMessageId, bodyStream, transportTransaction, numberOfDeliveryAttempts)
        {
            
        }
    }
}