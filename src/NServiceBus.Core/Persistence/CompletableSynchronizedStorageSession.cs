namespace NServiceBus.Persistence
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a storage session from point of view of the infrastructure.
    /// </summary>
    public interface CompletableSynchronizedStorageSession : SynchronizedStorageSession, IDisposable
    {
        /// <summary>
        /// Completes the session by saving the changes.
        /// </summary>
        Task CompleteAsync();
    }
}