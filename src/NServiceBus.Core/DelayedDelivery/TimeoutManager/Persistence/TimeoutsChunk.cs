namespace NServiceBus.Timeout.Core
{
    using System;

    /// <summary>
    /// Contains a collection of timeouts that are due and when to query for timeouts again.
    /// </summary>
    public class TimeoutsChunk
    {
        /// <summary>
        /// Creates a new instance of the timeouts chunk.
        /// </summary>
        /// <param name="dueTimeouts">timeouts that are due.</param>
        /// <param name="nextTimeToQuery">the next time to query for due timeouts again.</param>
        public TimeoutsChunk(Timeout[] dueTimeouts, DateTimeOffset nextTimeToQuery)
        {
            DueTimeouts = dueTimeouts;
            NextTimeToQuery = nextTimeToQuery;
        }

        /// <summary>
        /// timeouts that are due.
        /// </summary>
        public Timeout[] DueTimeouts { get; }

        /// <summary>
        /// the next time to query for due timeouts again.
        /// </summary>
        public DateTimeOffset NextTimeToQuery { get; }

        /// <summary>
        /// Represents a timeout.
        /// </summary>
        public struct Timeout
        {
            /// <summary>
            /// Creates a new instance of a timeout representation.
            /// </summary>
            /// <param name="id">The id of the timeout.</param>
            /// <param name="dueTime">The due time of the timeout.</param>
            public Timeout(string id, DateTimeOffset dueTime)
            {
                Id = id;
                DueTime = dueTime;
            }

            /// <summary>
            /// The id of the timeout.
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// The due time of the timeout.
            /// </summary>
            public DateTimeOffset DueTime { get; }
        }
    }
}