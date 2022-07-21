namespace NServiceBus.Persistence
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Pipeline;
    using Transport;

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
        public static Task Open(this CompletableSynchronizedStorageSession session, IIncomingLogicalMessageContext context)
        {
            var outboxTransaction = context.Extensions.Get<OutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();
            return session.Open(outboxTransaction, transportTransaction, context.Extensions);
        }

        /// <summary>
        /// Opens the storage session based on the outbox or the transport transaction.
        /// </summary>
        /// <param name="session">The storage session.</param>
        /// <param name="outboxTransaction">The outbox transaction.</param>
        /// <param name="transportTransaction">The transport transaction.</param>
        /// <param name="contextBag">The context bag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task Open(this CompletableSynchronizedStorageSession session,
            OutboxTransaction outboxTransaction, TransportTransaction transportTransaction,
            ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            // TODO: Add check instead of blunt cast
            var internalSession = (ICompletableSynchronizedStorageSession)session;
            if (await internalSession.TryOpen(outboxTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }
            if (await internalSession.TryOpen(transportTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }
            await internalSession.Open(contextBag).ConfigureAwait(false);
        }
    }
}