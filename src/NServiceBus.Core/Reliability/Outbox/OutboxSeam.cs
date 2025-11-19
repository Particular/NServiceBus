namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;

class OutboxSeam(IOutboxStorage outboxStorage, bool executeSetAsDispatched) : IOutboxSeam
{
    public Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        => outboxStorage.Get(messageId, context, cancellationToken);

    public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context,
        CancellationToken cancellationToken = default)
        => outboxStorage.Store(message, transaction, context, cancellationToken);

    public Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        => executeSetAsDispatched ? outboxStorage.SetAsDispatched(messageId, context, cancellationToken) : Task.CompletedTask;

    public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
        => outboxStorage.BeginTransaction(context, cancellationToken);
}