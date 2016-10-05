namespace NServiceBus
{
    using System.Threading;
    using Pipeline;
    using Transport;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    class TransportReceiveContext : BehaviorContext, ITransportReceiveContext
    {
        /// <summary>
        /// Creates a new transport receive context.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="transportTransaction">The transport transaction.</param>
        /// <param name="cancellationTokenSource">
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// It also allows the transport to communicate to the pipeline to abort if possible.
        /// </param>
        /// <param name="parentContext">The parent context.</param>
        public TransportReceiveContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, CancellationTokenSource cancellationTokenSource, IBehaviorContext parentContext)
            : base(parentContext)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            Message = receivedMessage;
            Set(Message);
            Set(transportTransaction);
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; }

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