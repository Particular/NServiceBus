namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Extensibility;

    /// <summary>
    /// Allows the transport to pass relevant info to the pipeline.
    /// </summary>
    public class PushContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="messageId">Native message id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="bodyStream">The message body stream.</param>
        /// <param name="transportTransaction">Transaction (along with connection if applicable) used to receive the message.</param>
        /// <param name="receiveCancellationTokenSource">Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back. 
        /// It also allows the transport to communicate to the pipeline to abort if possible. Transports should check if the token has been aborted after invoking the pipeline and roll back the message accordingly.</param>
        /// <param name="context">Any context that the transport wants to be available on the pipeline.</param>
        public PushContext(string messageId, Dictionary<string, string> headers, Stream bodyStream, TransportTransaction transportTransaction, CancellationTokenSource receiveCancellationTokenSource, ContextBag context)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(bodyStream), bodyStream);
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(transportTransaction), transportTransaction);
            Guard.AgainstNull(nameof(receiveCancellationTokenSource), receiveCancellationTokenSource);
            Guard.AgainstNull(nameof(context), context);

            Headers = headers;
            BodyStream = bodyStream;
            MessageId = messageId;
            Context = context;
            TransportTransaction = transportTransaction;
            ReceiveCancellationTokenSource = receiveCancellationTokenSource;
        }

        /// <summary>
        /// The native id of the message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The message body.
        /// </summary>
        public Stream BodyStream { get; }

        /// <summary>
        /// Transaction (along with connection if applicable) used to receive the message.
        /// </summary>
        public TransportTransaction TransportTransaction { get; }

        /// <summary>
        /// Allows the pipeline to flag that the pipeline has been aborted and the receive operation should be rolled back. 
        /// It also allows the transport to communicate to the pipeline to abort if possible.
        /// </summary>
        public CancellationTokenSource ReceiveCancellationTokenSource { get; }

        /// <summary>
        /// Context provided by the transport.
        /// </summary>
        public ContextBag Context { get; }
    }
}