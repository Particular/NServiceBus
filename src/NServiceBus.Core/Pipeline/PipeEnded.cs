namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Pipe execution ended.
    /// </summary>
    public struct PipeEnded
    {
        readonly string pipeId;
        readonly TimeSpan duration;

        /// <summary>
        /// Creates an instance of <see cref="PipeEnded"/>.
        /// </summary>
        /// <param name="pipeId">Pipe identifier.</param>
        /// <param name="duration">Elapsed time.</param>
        public PipeEnded(string pipeId, TimeSpan duration)
        {
            this.pipeId = pipeId;
            this.duration = duration;
        }

        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get { return pipeId; } }

        /// <summary>
        /// Elapsed time.
        /// </summary>
        public TimeSpan Duration { get { return duration; } }
    }
}