namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;

    class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }

        public bool WasDispatched { get; set; }
        
        public Task<OutboxMessage> Get(string messageId, ReadOnlyContextBag options)
        {
            if (ExistingMessage != null && ExistingMessage.MessageId == messageId)
            {
                return Task.FromResult(ExistingMessage);
            }

            return Task.FromResult(default(OutboxMessage));
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ReadOnlyContextBag options)
        {
            StoredMessage = message;
            return Task.FromResult(0);
        }

        public Task SetAsDispatched(string messageId, ReadOnlyContextBag options)
        {
            WasDispatched = true;
            return Task.FromResult(0);
        }

        public Task<OutboxTransaction> BeginTransaction(ReadOnlyContextBag context)
        {
            throw new System.NotImplementedException();
        }
    }
}
