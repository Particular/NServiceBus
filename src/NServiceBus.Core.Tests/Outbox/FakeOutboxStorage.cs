namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Outbox;

    internal class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            message = null;

            if (ExistingMessage != null && ExistingMessage.Id == messageId)
            {
                message = ExistingMessage;
                return true;
            }

            return false;
        }

        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            StoredMessage = new OutboxMessage(messageId);
            StoredMessage.TransportOperations.AddRange(transportOperations);
        }

        public void SetAsDispatched(string messageId)
        {
            if (throwOnDispatch)
            {
                throw new InvalidOperationException("Dispatch should not be invoked");
            }

            if (StoredMessage != null)
            {
                if (StoredMessage.Id == messageId)
                {
                    StoredMessage = new OutboxMessage(messageId, true);
                }
            }

            if (ExistingMessage != null)
            {
                if (ExistingMessage.Id == messageId)
                {
                    ExistingMessage = new OutboxMessage(messageId, true);
                }
            }
        }

        public void ThrowOnDispatch()
        {
            throwOnDispatch = true;
        }

        bool throwOnDispatch;
    }
}