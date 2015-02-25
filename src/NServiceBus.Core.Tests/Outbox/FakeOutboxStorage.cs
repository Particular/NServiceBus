namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.Outbox;

    internal class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }

        public bool WasDispatched { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            message = null;

            if (ExistingMessage != null && ExistingMessage.MessageId == messageId)
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
            WasDispatched = true;
        }
    }
}