namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows to query for timeouts.
    /// </summary>
    public interface IQueryTimeouts
    {
        /// <summary>
        /// Retrieves the next range of timeouts that are due.
        /// </summary>
        /// <param name="startSlice">The time where to start retrieving the next slice, the slice should exclude this date.</param>
        /// <param name="nextTimeToRunQuery">Returns the next time we should query again.</param>
        /// <returns>Returns the next range of timeouts that are due.</returns>
        IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery);
    }
}