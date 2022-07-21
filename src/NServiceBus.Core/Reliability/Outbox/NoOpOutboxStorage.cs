namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    class NoOpOutboxStorage : IOutboxStorage
    {
        public Task<OutboxMessage> Get(string messageId, ContextBag options) => NoOutboxMessageTask;

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag options) => TaskEx.CompletedTask;

        public Task SetAsDispatched(string messageId, ContextBag options) => TaskEx.CompletedTask;

        public Task<OutboxTransaction> BeginTransaction(ContextBag context) => NoOutboxTransactionTask;

        static Task<OutboxMessage> NoOutboxMessageTask = Task.FromResult<OutboxMessage>(null);
        static Task<OutboxTransaction> NoOutboxTransactionTask = Task.FromResult<OutboxTransaction>(new NoOpOutboxTransaction());
    }
}