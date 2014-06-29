namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Pipe execution ended.
    /// </summary>
    public struct PipeEnded
    {
        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get; set; }

        /// <summary>
        /// Elapsed time.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}