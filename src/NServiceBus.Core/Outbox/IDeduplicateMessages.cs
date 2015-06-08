namespace NServiceBus.Outbox
{
    /// <summary>
    /// Implemented by the persisters to message deduplication capabilities
    /// </summary>
    public interface IDeduplicateMessages
    {
        /// <summary>
        /// Tries to find the given message in the outbox
        /// </summary>
        bool TryGet(string messageId, out OutboxMessage message);
        
        /// <summary>
        /// Tells the storage that the message has been dispatched and its now safe to clean up the transport operations
        /// </summary>
        void SetAsDispatched(string messageId);
    }
}