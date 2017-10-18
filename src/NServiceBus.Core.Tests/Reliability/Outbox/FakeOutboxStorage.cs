namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;

    class FakeOutboxStorage : IOutboxStorage
    {
        public OutboxMessage ExistingMessage { get; set; }
        public OutboxMessage StoredMessage { get; set; }

        public bool WasDispatched { get; set; }
        
        public Task<OutboxMessage> Get(string messageId, ContextBag options)
        {
            if (ExistingMessage != null && ExistingMessage.MessageId == messageId)
            {
                return Task.FromResult(ExistingMessage);
            }

            return Task.FromResult(default(OutboxMessage));
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag options)
        {
            StoredMessage = message;
            return TaskEx.CompletedTask;
        }

        public Task SetAsDispatched(string messageId, ContextBag options)
        {
            WasDispatched = true;
            return TaskEx.CompletedTask;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            return Task.FromResult<OutboxTransaction>(new Transaction());
        }

        class Transaction : OutboxTransaction
        {
            public void Dispose()
            {
            }

            public Task Commit()
            {
                return Task.CompletedTask;
            }
        }
    }
}
