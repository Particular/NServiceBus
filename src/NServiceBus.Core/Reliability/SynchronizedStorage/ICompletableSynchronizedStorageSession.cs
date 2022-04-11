namespace NServiceBus.Persistence
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Transport;

    /// <summary>
    /// Represents a storage session from point of view of the infrastructure.
    /// </summary>
    public interface ICompletableSynchronizedStorageSession : ISynchronizedStorageSession, IDisposable
    {
        /// <summary>
        /// Begins a new storage session which is an atomic unit of work.
        /// </summary>
        /// <param name="transaction">Outbox transaction.</param>
        /// <param name="context">Context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        ValueTask<bool> OpenSession(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new storage session which is an atomic unit of work.
        /// </summary>
        /// <param name="transportTransaction">Transport transaction.</param>
        /// <param name="context">Context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns><c>true</c> when the session was opened; otherwise <c>false</c>.</returns>
        ValueTask<bool> OpenSession(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new storage session which is an atomic unit of work.
        /// </summary>
        /// <param name="contextBag">The context information.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        ValueTask OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes the session by saving the changes.
        /// </summary>
        Task CompleteAsync(CancellationToken cancellationToken = default);
    }
}
