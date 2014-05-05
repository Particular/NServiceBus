namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;

    public interface IOutboxStorage
    {
        bool TryGet(string messageId, out OutboxMessage message);
        IDisposable OpenSession();
        void StoreAndCommit(string messageId, IEnumerable<TransportOperation> transportOperations);
        void SetAsDispatched(string messageId);
    }
}