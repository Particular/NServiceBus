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
        /// <returns>Returns the next range of timeouts that are due.</returns>
        List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery);

        /// <summary>
        /// Adds a new timeout.
        /// </summary>
        /// <param name="timeoutId">The timeout id.</param>
        /// /// <param name="timeout">Timeout data.</param>
        void Add(string timeoutId,TimeoutData timeout);

        /// <summary>
        /// Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <param name="timeoutData">The timeout data of the removed timeout.</param>
        /// <returns><c>true</c> it the timeout was successfully removed.</returns>
        bool TryRemove(string timeoutId, out TimeoutData timeoutData);

        /// <summary>
        /// Removes the time by saga id.
        /// </summary>
        /// <param name="sagaId">The saga id of the timeouts to remove.</param>
        void RemoveTimeoutBy(Guid sagaId);
    }
}
