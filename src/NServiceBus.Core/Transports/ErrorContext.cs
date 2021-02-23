namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using Extensibility;

    /// <summary>
    /// The context for messages that has failed processing.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// Initializes the error context.
        /// </summary>
        /// <param name="exception">The exception that caused the message processing failure.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="transportMessageId">Native message id.</param>
        /// <param name="body">The message body.</param>
        /// <param name="transportTransaction">Transaction (along with connection if applicable) used to receive the message.</param>
        /// <param name="immediateProcessingFailures">Number of failed immediate processing attempts.</param>
        /// <param name="context">A <see cref="ReadOnlyContextBag" /> which can be used to extend the current object.</param>
        public ErrorContext(Exception exception, Dictionary<string, string> headers, string transportMessageId, byte[] body, TransportTransaction transportTransaction, int immediateProcessingFailures, ReadOnlyContextBag context)
        {
            Exception = exception;
            TransportTransaction = transportTransaction;
            ImmediateProcessingFailures = immediateProcessingFailures;

            Message = new IncomingMessage(transportMessageId, headers, body);

            DelayedDeliveriesPerformed = Message.GetDelayedDeliveriesPerformed();
            Extensions = context;
        }

        /// <summary>
        /// Exception that caused the message processing to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Transport transaction for failed receive message.
        /// </summary>
        public TransportTransaction TransportTransaction { get; }

        /// <summary>
        /// Number of failed immediate processing attempts. This number is re-set with each delayed delivery.
        /// </summary>
        public int ImmediateProcessingFailures { get; }

        /// <summary>
        /// Number of delayed deliveries performed so far.
        /// </summary>
        public int DelayedDeliveriesPerformed { get; }

        /// <summary>
        /// Failed incoming message.
        /// </summary>
        public IncomingMessage Message { get; }

        /// <summary>
        /// A collection of additional information provided by the transport.
        /// </summary>
        public ReadOnlyContextBag Extensions { get; }
    }
}
