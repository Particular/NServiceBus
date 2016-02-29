namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
    {
        /// <summary>
        /// Creates a new transport receive context.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="bodyStream">The message body stream.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="transportTransaction">The transport transaction.</param>
        /// <param name="cancellationTokenSource">
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// It also allows the transport to communicate to the pipeline to abort if possible.
        /// </param>
        /// <param name="parentContext">The parent context.</param>
        public TransportReceiveContext(string messageId, Dictionary<string, string> headers, Stream bodyStream, TransportTransaction transportTransaction, CancellationTokenSource cancellationTokenSource, IBehaviorContext parentContext)
            : base(parentContext)
        {
            MessageId = messageId;
            Headers = headers;
            Body = new byte[bodyStream.Length];
            bodyStream.Read(Body, 0, Body.Length);

            this.cancellationTokenSource = cancellationTokenSource;
            Set(transportTransaction);
        }

        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }
        public byte[] Body { get; internal set; } // Daniel: Intermediate hack with internal set

        public void RevertToOriginalBodyIfNeeded()
        {
        }

        /// <summary>
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// </summary>
        public void AbortReceiveOperation()
        {
            cancellationTokenSource.Cancel();
        }

        CancellationTokenSource cancellationTokenSource;
    }
}