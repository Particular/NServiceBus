namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows to query for timeouts.
    /// </summary>
    public interface IQueryTimeouts
    {
        /// <summary>
        /// Retrieves the next range of timeouts that are due.
        /// </summary>
        /// <param name="startSlice">The time where to start retrieving the next slice, the slice should exclude this date.</param>
        /// <returns>Returns the next range of timeouts that are due.</returns>
        Task<TimeoutsChunk> GetNextChunk(DateTime startSlice);
    }
}