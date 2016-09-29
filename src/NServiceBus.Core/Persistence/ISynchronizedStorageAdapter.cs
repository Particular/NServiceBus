namespace NServiceBus.Persistence
{
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
        /// <returns>Session or null, if unable to adapt.</returns>
        Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context);

        /// <summary>
        /// Returns a synchronized storage session based on the outbox transaction if possible.
        /// </summary>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="context">Context.</param>
        /// <returns>Session or null, if unable to adapt.</returns>
        Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context);
    }
}