﻿namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using Extensibility;

    /// <summary>
    /// The context for messages that has failed processing.
    /// </summary>
    public class ErrorContext : IExtendable
    {

        /// <summary>
        /// Initializes the error context.
        /// </summary>
        public ErrorContext(Exception exception, Dictionary<string, string> headers, string transportMessageId, byte[] body, TransportTransaction transportTransaction, int immediateProcessingFailures)
        : this(exception, headers, transportMessageId, body, transportTransaction, immediateProcessingFailures, new ContextBag())
        {
        }

        /// <summary>
        /// Initializes the error context.
        /// </summary>
        public ErrorContext(Exception exception, Dictionary<string, string> headers, string transportMessageId, byte[] body, TransportTransaction transportTransaction, int immediateProcessingFailures, ContextBag context)
        {
            Exception = exception;
            TransportTransaction = transportTransaction;
            ImmediateProcessingFailures = immediateProcessingFailures;

            Message = new IncomingMessage(transportMessageId, headers, body);

            DelayedDeliveriesPerformed = Message.GetDelayedDeliveriesPerformed();
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
        /// 
        /// </summary>
        public ContextBag Extensions { get; }
    }
}
