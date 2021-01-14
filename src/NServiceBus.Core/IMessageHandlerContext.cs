namespace NServiceBus
{
    using Persistence;
    using System.Threading;

    /// <summary>
    /// The context of the currently processed message for a message handler.
    /// </summary>
    public interface IMessageHandlerContext : IMessageProcessingContext
    {
        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        SynchronizedStorageSession SynchronizedStorageSession { get; }

        /// <summary>
        /// Tells the endpoint to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();

        /// <summary>
        /// A <see cref="CancellationToken"/> to observe during message processing. Should be forwarded to other methods within the message handler that support cancellation.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}