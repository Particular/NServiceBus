namespace NServiceBus
{
    using System.Threading.Tasks;
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
        SynchronizedStorageSession SynchronizedStorageSession { get; }

        /// <summary>
        /// Moves the message being handled to the back of the list of available
        /// messages so it can be handled later.
        /// </summary>
        Task HandleCurrentMessageLater();

        /// <summary>
        /// Tells the endpoint to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();
    }
}