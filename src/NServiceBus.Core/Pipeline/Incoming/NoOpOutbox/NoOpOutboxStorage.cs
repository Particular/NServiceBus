namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    class NoOpOutboxStorage : IOutboxStorage
    {
        public Task<OutboxMessage> Get(string messageId, ContextBag options, CancellationToken cancellationToken = default)
        {
            return NoOutboxMessageTask;
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SetAsDispatched(string messageId, ContextBag options, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OutboxTransaction>(new NoOpOutboxTransaction());
        }

        static Task<OutboxMessage> NoOutboxMessageTask = Task.FromResult<OutboxMessage>(null);
    }
}