namespace NServiceBus.Core.Tests.Pipeline
{
    class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage Get(string messageId)
        {
            if (ExistingMessage != null && ExistingMessage.Id == messageId)
                return ExistingMessage;

            return null;
        }

        public void Store(OutboxMessage outboxMessage)
        {
            StoredMessage = outboxMessage;
        }

        public void SetAsDispatched(OutboxMessage outboxMessage)
        {
            if (StoredMessage != null)
            {
                if (StoredMessage.Id == outboxMessage.Id)
                    StoredMessage.Dispatched = true;
            }

            if (ExistingMessage != null)
            {
                if (ExistingMessage.Id == outboxMessage.Id)
                    ExistingMessage.Dispatched = true;
            }
        }

        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }
    }
}