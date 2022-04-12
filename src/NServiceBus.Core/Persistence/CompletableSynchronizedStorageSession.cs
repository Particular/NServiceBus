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
#pragma warning disable IDE1006 // Naming Styles
    public interface CompletableSynchronizedStorageSession : SynchronizedStorageSession, IDisposable
#pragma warning restore IDE1006 // Naming Styles
    {
        /// <summary>
        /// Completes the session by saving the changes.
        /// </summary>
        Task CompleteAsync();
    }

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
        Task<bool> TryOpenSession(OutboxTransaction transaction, ContextBag context);

        /// <summary>
        /// Tries to open the storage session with the provided transport transaction.
        /// </summary>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="context">Context.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        Task<bool> TryOpenSession(TransportTransaction transportTransaction, ContextBag context);

        /// <summary>
        /// Opens the storage session.
        /// </summary>
        /// <param name="contextBag">The context information.</param>
        Task OpenSession(ContextBag contextBag);

        /// <summary>
        /// Completes the session by saving the changes.
        /// </summary>
        Task CompleteAsync();
    }

    static class CompletableSynchronizedStorageSessionExtensions
    {
        public static async Task OpenSession(this ICompletableSynchronizedStorageSession session, OutboxTransaction outboxTransaction, TransportTransaction transportTransaction,
            ContextBag contextBag)
        {
            if (await session.TryOpenSession(outboxTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }
            if (await session.TryOpenSession(transportTransaction, contextBag).ConfigureAwait(false))
            {
                return;
            }
            await session.OpenSession(contextBag).ConfigureAwait(false);
        }
    }
}