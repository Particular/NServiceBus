#nullable enable

namespace NServiceBus.Persistence;

using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;
using Pipeline;
using Transport;

// TODO(nullable-major-release): Investigate if these should even be public. If they should, then they need to have argument validtion added.

/// <summary>
/// Extension methods for <see cref="ICompletableSynchronizedStorageSession"/>.
/// </summary>
public static class CompletableSynchronizedStorageSessionExtensions
{
    /// <summary>
    /// Opens the storage session by attempting to extract the outbox and transport transaction from the incoming logical context.
    /// </summary>
    /// <param name="session">The storage session.</param>
    /// <param name="context">The context information.</param>
    public static ValueTask Open(this ICompletableSynchronizedStorageSession session, IIncomingLogicalMessageContext context)
    {
        // An outbox transaction is only in the context when an outbox is configured. Substituting the no-op
        // transaction keeps this contract non-nullable without the receive pipeline having to park a
        // placeholder in the context on every message.
        if (!context.Extensions.TryGet<IOutboxTransaction>(out var outboxTransaction))
        {
            outboxTransaction = NoOpOutboxTransaction.Instance;
        }

        var transportTransaction = context.Extensions.Get<TransportTransaction>();
        return session.Open(outboxTransaction, transportTransaction, context.Extensions, context.CancellationToken);
    }

    /// <summary>
    /// Opens the storage session based on the outbox or the transport transaction.
    /// </summary>
    /// <param name="session">The storage session.</param>
    /// <param name="outboxTransaction">The outbox transaction.</param>
    /// <param name="transportTransaction">The transport transaction.</param>
    /// <param name="contextBag">The context bag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async ValueTask Open(this ICompletableSynchronizedStorageSession session,
        IOutboxTransaction outboxTransaction, TransportTransaction transportTransaction,
        ContextBag contextBag, CancellationToken cancellationToken = default)
    {
        if (await session.TryOpen(outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false))
        {
            return;
        }
        if (await session.TryOpen(transportTransaction, contextBag, cancellationToken).ConfigureAwait(false))
        {
            return;
        }
        await session.Open(contextBag, cancellationToken).ConfigureAwait(false);
    }
}