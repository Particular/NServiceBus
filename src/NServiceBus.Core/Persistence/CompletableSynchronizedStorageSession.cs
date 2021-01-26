namespace NServiceBus.Persistence
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

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
        Task CompleteAsync(CancellationToken cancellationToken);
    }
}