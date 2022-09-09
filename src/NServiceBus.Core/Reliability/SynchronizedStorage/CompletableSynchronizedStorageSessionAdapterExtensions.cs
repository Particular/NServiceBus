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
    public static class CompletableSynchronizedStorageSessionAdapterExtensions
    {
        /// <summary>
        /// Opens the storage session by attempting to extract the outbox and transport transaction from the incoming logical context.
        /// </summary>
        /// <param name="sessionAdapter">The storage session.</param>
        /// <param name="context">The context information.</param>
        public static Task Open(this CompletableSynchronizedStorageSessionAdapter sessionAdapter,
            IIncomingLogicalMessageContext context)
        {
            var outboxTransaction = context.Extensions.Get<OutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();
            return sessionAdapter.Open(outboxTransaction, transportTransaction, context.Extensions);
        }

        /// <summary>
        /// Opens the storage session based on the outbox or the transport transaction.
        /// </summary>
        /// <param name="sessionAdapter">The storage session.</param>
        /// <param name="outboxTransaction">The outbox transaction.</param>
        /// <param name="transportTransaction">The transport transaction.</param>
        /// <param name="contextBag">The context bag.</param>
        public static async Task Open(this CompletableSynchronizedStorageSessionAdapter sessionAdapter,
            OutboxTransaction outboxTransaction, TransportTransaction transportTransaction,
            ContextBag contextBag)
        {
            if (await sessionAdapter.TryOpen(outboxTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }

            if (await sessionAdapter.TryOpen(transportTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }

            await sessionAdapter.Open(contextBag).ConfigureAwait(false);
        }
    }
}