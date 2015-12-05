namespace NServiceBus.Persistence
{
    using System;

    /// <summary>
    /// Represents a storage session.
    /// </summary>
    public interface ISynchronizedStorageSession
    {
        /// <summary>
        /// Adds a change to a session.
        /// </summary>
        void Enlist(Action action);
    }
}