namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Threading;
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
        /// <param name="maxChunkSize">The maximum chunk size that the caller specifies to limit the number of results.</param>
        /// <param name="cancellationToken">The cancellation token used by the caller to notify that the pending work should be cancelled.</param>
        /// <returns>
        /// Returns the next range of timeouts that are due.
        /// </returns>
        Task<TimeoutsChunk> GetNextChunk(DateTime startSlice, int maxChunkSize = Int32.MaxValue, CancellationToken cancellationToken = default(CancellationToken));
    }
}