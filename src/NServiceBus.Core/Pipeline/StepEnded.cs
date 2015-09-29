namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    ///     Step execution ended.
    /// </summary>
    public struct StepEnded
    {
        /// <summary>
        ///     Creates an instance of <see cref="StepEnded" />.
        /// </summary>
        /// <param name="duration">Elapsed time.</param>
        public StepEnded(TimeSpan duration)
        {
            Duration = duration;
        }

        /// <summary>
        ///     Elapsed time.
        /// </summary>
        public TimeSpan Duration { get; }
    }
}