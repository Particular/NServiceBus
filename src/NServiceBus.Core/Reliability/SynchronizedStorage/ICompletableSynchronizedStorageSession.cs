namespace NServiceBus.Persistence
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Transport;

    /// <summary>
    /// Represents a storage session from point of view of the infrastructure.
    /// </summary>
    public interface ICompletableSynchronizedStorageSession : SynchronizedStorageSession, IDisposable
    {
        /// <summary>
        /// Tries to open the storage session with the provided outbox transaction.
        /// </summary>
        /// <param name="transaction">Outbox transaction.</param>
        /// <param name="context">Context.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        Task<bool> TryOpen(OutboxTransaction transaction, ContextBag context);

        /// <summary>
        /// Tries to open the storage session with the provided transport transaction.
        /// </summary>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="context">Context.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        Task<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context);

        /// <summary>
        /// Opens the storage session.
        /// </summary>
        /// <param name="contextBag">The context information.</param>
        Task Open(ContextBag contextBag);

        /// <summary>
        /// Completes the session by saving the changes.
        /// </summary>
        Task CompleteAsync();
    }
}