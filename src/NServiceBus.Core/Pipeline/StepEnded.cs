namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Step execution ended.
    /// </summary>
    public struct StepEnded
    {
        readonly string pipeId;
        readonly string stepId;
        readonly TimeSpan duration;

        /// <summary>
        /// Creates an instance of <see cref="PipeEnded"/>.
        /// </summary>
        /// <param name="pipeId">Pipe identifier.</param>
        /// <param name="stepId">Step identifier.</param>
        /// <param name="duration">Elapsed time.</param>
        public StepEnded(string pipeId, string stepId, TimeSpan duration)
        {
            this.pipeId = pipeId;
            this.stepId = stepId;
            this.duration = duration;
        }

        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get { return pipeId; } }

        /// <summary>
        /// Step identifier.
        /// </summary>
        public string StepId { get { return stepId; } }

        /// <summary>
        /// Elapsed time.
        /// </summary>
        public TimeSpan Duration { get { return duration; } }
    }
}