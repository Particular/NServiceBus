namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;

    /// <summary>
    /// Extension methods for <see cref="ICompletableSynchronizedStorageSession"/>.
    /// </summary>
    static class CompletableSynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Opens the storage session by attempting to extract the outbox and transport transaction from the incoming logical context.
        /// </summary>
        /// <param name="session">The storage session.</param>
        /// <param name="context">The context information.</param>
        public static Task Open(this ICompletableSynchronizedStorageSession session, IIncomingLogicalMessageContext context)
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
        public static async Task Open(this ICompletableSynchronizedStorageSession session, OutboxTransaction outboxTransaction, TransportTransaction transportTransaction, ContextBag contextBag)
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
    }
}