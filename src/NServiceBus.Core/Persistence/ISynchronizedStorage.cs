namespace NServiceBus.Persistence
{
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Represents a storage to which the writes are synchronized with message receiving i.e. message receive is acknowledged
    /// only if data has been successfully saved.
    /// </summary>
    public interface ISynchronizedStorage
    {
        /// <summary>
        /// Begins a new storage session which is an atomic unit of work.
        /// </summary>
        /// <param name="contextBag">The context information.</param>
        Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag);
    }
}