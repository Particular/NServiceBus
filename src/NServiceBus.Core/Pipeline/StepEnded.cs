namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Step execution ended.
    /// </summary>
    public struct StepEnded
    {
        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get; set; }

        /// <summary>
        /// Step identifier.
        /// </summary>
        public string StepId { get; internal set; }

        /// <summary>
        /// Elapsed time.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}