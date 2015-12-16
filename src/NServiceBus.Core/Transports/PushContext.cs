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
        /// <param name="tokenSource">The cancellation token source.</param>
        /// <param name="context">Any context that the transport wants to be available on the pipeline.</param>
        public PushContext(string messageId, Dictionary<string, string> headers, Stream bodyStream, TransportTransaction transportTransaction, CancellationTokenSource tokenSource, ContextBag context)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(bodyStream), bodyStream);
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(transportTransaction), transportTransaction);
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(tokenSource), tokenSource);

            Headers = headers;
            BodyStream = bodyStream;
            MessageId = messageId;
            Context = context;
            TransportTransaction = transportTransaction;

            Context.Set(tokenSource);
        }

        /// <summary>
        /// The native id of the message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// The message body.
        /// </summary>
        public Stream BodyStream { get; private set; }

        /// <summary>
        /// Context provided by the transport.
        /// </summary>
        public ContextBag Context { get; private set; }

        /// <summary>
        /// Transaction (along with connection if applicable) used to receive the message.
        /// </summary>
        public TransportTransaction TransportTransaction { get; private set; }
    }
}