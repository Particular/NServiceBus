namespace NServiceBus.Persistence
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Pipeline;
    using Transport;

    /// <summary>
    /// Extension methods for <see cref="CompletableSynchronizedStorageSession"/>.
    /// </summary>
    public static class CompletableSynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Opens the storage session by attempting to extract the outbox and transport transaction from the incoming logical context.
        /// </summary>
        /// <param name="session">The storage session.</param>
        /// <param name="context">The context information.</param>
        public static Task Open(this CompletableSynchronizedStorageSession session,
            IIncomingLogicalMessageContext context)
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
        public static async Task Open(this CompletableSynchronizedStorageSession session,
            OutboxTransaction outboxTransaction, TransportTransaction transportTransaction,
            ContextBag contextBag)
        {
            if (await session.TryOpen(outboxTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }

            if (await session.TryOpen(transportTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }

            await session.Open(contextBag).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to open the storage session with the provided outbox transaction.
        /// </summary>
        /// <param name="session">The storage session.</param>
        /// <param name="transaction">Outbox transaction.</param>
        /// <param name="context">Context.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        public static Task<bool> TryOpen(this CompletableSynchronizedStorageSession session,
            OutboxTransaction transaction, ContextBag context) =>
            ((CompletableSynchronizedStorageSessionAdapter)session).TryOpen(transaction, context);

        /// <summary>
        /// Tries to open the storage session with the provided transport transaction.
        /// </summary>
        /// <param name="session">The storage session.</param>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="context">Context.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        public static Task<bool> TryOpen(this CompletableSynchronizedStorageSession session,
            TransportTransaction transportTransaction, ContextBag context) =>
            ((CompletableSynchronizedStorageSessionAdapter)session).TryOpen(transportTransaction, context);

        /// <summary>
        /// Opens the storage session.
        /// </summary>
        /// <param name="session">The storage session.</param>
        /// <param name="context">The context information.</param>
        public static Task Open(this CompletableSynchronizedStorageSession session, ContextBag context) =>
            ((CompletableSynchronizedStorageSessionAdapter)session).Open(context);

    }
}