namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    /// <summary>
    /// Implemented by the persisters to provide outbox storage capabilities
    /// </summary>
    public interface IStoreOutboxMessages
    {
        /// <summary>
        /// Stores 
        /// </summary>
        void Store(string messageId, IEnumerable<TransportOperation> transportOperations);
    }
}