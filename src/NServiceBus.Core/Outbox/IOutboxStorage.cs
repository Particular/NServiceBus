namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    public interface IOutboxStorage
    {
        bool TryGet(string messageId, out OutboxMessage message);
        void Store(string messageId, IEnumerable<TransportOperation> transportOperations);
        void SetAsDispatched(string messageId);
    }
}