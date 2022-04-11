namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    class NoOpOutboxStorage : IOutboxStorage
    {
        public Task<OutboxMessage> Get(string messageId, ContextBag options, CancellationToken cancellationToken = default) => NoOutboxMessageTask;

        public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag options, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetAsDispatched(string messageId, ContextBag options, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) => NoOutboxTransactionTask;

        static Task<OutboxMessage> NoOutboxMessageTask = Task.FromResult<OutboxMessage>(null);
        static Task<IOutboxTransaction> NoOutboxTransactionTask = Task.FromResult<IOutboxTransaction>(new NoOpOutboxTransaction());
    }
}