namespace NServiceBus.Persistence
{
    using NServiceBus.Outbox;
    using NServiceBus.Transports;

    /// <summary>
    /// Converts the outbox transaction into a synchronized storage session if possible.
    /// </summary>
    public interface ISynchronizedStorageAdapter
    {
        /// <summary>
        /// Returns a synchronized storage session based on the outbox transaction if possible. 
        /// </summary>
        /// <param name="transaction">Outbox transaction.</param>
        /// <param name="session">Session or null, if unable to adapt.</param>
        /// <returns></returns>
        bool TryAdapt(OutboxTransaction transaction, out ICompletableSynchronizedStorageSession session);

        /// <summary>
        /// Returns a synchronized storage session based on the outbox transaction if possible. 
        /// </summary>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="session">Session or null, if unable to adapt.</param>
        /// <returns></returns>
        bool TryAdapt(TransportTransaction transportTransaction, out ICompletableSynchronizedStorageSession session);
    }
}