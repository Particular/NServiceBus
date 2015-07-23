namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    /// <summary>
    /// Implemented by the persisters to provide outbox storage capabilities.
    /// </summary>
    public interface IOutboxStorage
    {
        /// <summary>
        /// Tries to find the given message in the outbox.
        /// </summary>
        bool TryGet(string messageId, out OutboxMessage message);

        /// <summary>
        /// Stores.
        /// </summary>
        void Store(string messageId, IEnumerable<TransportOperation> transportOperations);
        
        
        /// <summary>
        /// Tells the storage that the message has been dispatched and its now safe to clean up the transport operations.
        /// </summary>
        void SetAsDispatched(string messageId);
    }
}