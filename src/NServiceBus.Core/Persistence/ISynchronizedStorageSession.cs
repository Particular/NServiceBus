namespace NServiceBus.Persistence
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a storage session.
    /// </summary>
    public interface ISynchronizedStorageSession
    {
        /// <summary>
        /// Adds a change to a session.
        /// </summary>
        Task Enlist<T>(Func<T, Task> action);
    }
}