namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;

interface IOutboxSeam
{
    Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default);
    Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default);
    Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default);
    Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default);
}