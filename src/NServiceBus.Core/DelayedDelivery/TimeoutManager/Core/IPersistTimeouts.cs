namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Timeout persister contract.
    /// </summary>
    public interface IPersistTimeouts
    {
        /// <summary>
        /// Retrieves the next range of timeouts that are due.
        /// </summary>
        /// <param name="startSlice">The time where to start retrieving the next slice, the slice should exclude this date.</param>
        /// <param name="nextTimeToRunQuery">Returns the next time we should query again.</param>
        /// <param name="options">The timeout persistence options</param>
        /// <returns>Returns the next range of timeouts that are due.</returns>
        IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, TimeoutPersistenceOptions options, out DateTime nextTimeToRunQuery);

        /// <summary>
        /// Adds a new timeout.
        /// </summary>
        /// <param name="timeout">Timeout data.</param>
        /// <param name="options">The timeout persistence options</param>
        void Add(TimeoutData timeout, TimeoutPersistenceOptions options);

        /// <summary>
        /// Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <param name="timeoutData">The timeout data of the removed timeout.</param>
        /// <param name="options">The timeout persistence options</param>
        /// <returns><c>true</c> it the timeout was successfully removed.</returns>
        bool TryRemove(string timeoutId, TimeoutPersistenceOptions options, out TimeoutData timeoutData);

        /// <summary>
        /// Removes the time by saga id.
        /// </summary>
        /// <param name="sagaId">The saga id of the timeouts to remove.</param>
        /// <param name="options">The timeout persistence options</param>
        void RemoveTimeoutBy(Guid sagaId, TimeoutPersistenceOptions options);
    }
}
