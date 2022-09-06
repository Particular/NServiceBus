namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Outbox;
    using Persistence;
    using Transport;

    /// <summary>
    /// Wraps the logic to open a <see cref="CompletableSynchronizedStorageSession"/> via <see cref="ISynchronizedStorageAdapter"/> and <see cref="ISynchronizedStorage"/>.
    /// </summary>
    [SkipWeaving]
    public sealed class CompletableSynchronizedStorageSessionAdapter : IDisposable
    {
        /// <summary>
        /// Creates a new instance of <see cref="CompletableSynchronizedStorageSessionAdapter"/>.
        /// </summary>
        public CompletableSynchronizedStorageSessionAdapter(ISynchronizedStorageAdapter synchronizedStorageAdapter, ISynchronizedStorage synchronizedStorage)
        {
            this.synchronizedStorage = synchronizedStorage;
            this.synchronizedStorageAdapter = synchronizedStorageAdapter;
        }

        /// <summary>
        /// Access to the <see cref="CompletableSynchronizedStorageSession"/> once it has been opened.
        /// </summary>
        public CompletableSynchronizedStorageSession AdaptedSession { get; private set; }

        /// <summary>
        /// Disposes the adapter and the underlying <see cref="CompletableSynchronizedStorageSession"/>.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            AdaptedSession?.Dispose();
            disposed = true;
        }

        /// <summary>
        /// Completes the underlying <see cref="CompletableSynchronizedStorageSession"/>.
        /// </summary>
        public Task CompleteAsync() => AdaptedSession.CompleteAsync();

        /// <summary>
        /// Tries to open a <see cref="CompletableSynchronizedStorageSession"/> based on a given <see cref="OutboxTransaction"/>.
        /// </summary>
        public async Task<bool> TryOpen(OutboxTransaction transaction, ContextBag context)
        {
            AdaptedSession = await synchronizedStorageAdapter.TryAdapt(transaction, context).ConfigureAwait(false);

            return AdaptedSession != null;
        }

        /// <summary>
        /// Tries to open a <see cref="CompletableSynchronizedStorageSession"/> based on a given <see cref="TransportTransaction"/>.
        /// </summary>
        public async Task<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context)
        {
            AdaptedSession = await synchronizedStorageAdapter.TryAdapt(transportTransaction, context).ConfigureAwait(false);

            return AdaptedSession != null;
        }

        /// <summary>
        /// Opens a <see cref="CompletableSynchronizedStorageSession"/> via the persister that is not connected to an outbox or transport transaction.
        /// </summary>
        public async Task Open(ContextBag contextBag) => AdaptedSession = await synchronizedStorage.OpenSession(contextBag).ConfigureAwait(false);

        bool disposed;

        readonly ISynchronizedStorageAdapter synchronizedStorageAdapter;
        readonly ISynchronizedStorage synchronizedStorage;
    }
}