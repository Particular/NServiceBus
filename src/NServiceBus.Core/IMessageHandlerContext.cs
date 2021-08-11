namespace NServiceBus
{
    using Persistence;

    /// <summary>
    /// The context of the currently processed message for a message handler.
    /// </summary>
    public interface IMessageHandlerContext : IMessageProcessingContext
    {
        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        ISynchronizedStorageSession SynchronizedStorageSession { get; }

        /// <summary>
        /// Tells the endpoint to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();
    }
}