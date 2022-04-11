namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;

    static class CompletableSynchronizedStorageSessionExtensions
    {
        public static ValueTask OpenSession(this ICompletableSynchronizedStorageSession session, IIncomingLogicalMessageContext context)
        {
            var outboxTransaction = context.Extensions.Get<IOutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();
            return session.OpenSession(outboxTransaction, transportTransaction, context.Extensions, context.CancellationToken);
        }

        static async ValueTask OpenSession(this ICompletableSynchronizedStorageSession session, IOutboxTransaction outboxTransaction, TransportTransaction transportTransaction,
            ContextBag contextBag, CancellationToken cancellationToken)
        {
            if (await session.OpenSession(outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
            if (await session.OpenSession(transportTransaction, contextBag, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
            await session.OpenSession(contextBag, cancellationToken).ConfigureAwait(false);
        }
    }
}