namespace NServiceBus.Persistence
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Transport;

    /// <summary>
    /// Converts the outbox transaction into a synchronized storage session if possible.
    /// </summary>
    public interface ISynchronizedStorageAdapter
    {
        /// <summary>
        /// Returns a synchronized storage session based on the outbox transaction if possible.
        /// </summary>
        /// <param name="transaction">Outbox transaction.</param>
        /// <param name="context">Context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>Session or null, if unable to adapt.</returns>
        Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a synchronized storage session based on the outbox transaction if possible.
        /// </summary>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="context">Context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>Session or null, if unable to adapt.</returns>
        Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default);
    }
}