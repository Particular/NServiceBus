namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides information about the delayed retries configuration.
    /// </summary>
    public struct DelayedConfig
    {
        internal DelayedConfig(int maxNumberOfRetries, TimeSpan timeIncrease)
        {
            MaxNumberOfRetries = maxNumberOfRetries;
            TimeIncrease = timeIncrease;
        }

        /// <summary>
        /// Gets the configured maximum number of immediate retries.
        /// </summary>
        /// <remarks>Zero means no retries possible.</remarks>
        public int MaxNumberOfRetries { get; }

        /// <summary>
        /// Gets the configured time of increase for individual delayed retries.
        /// </summary>
        public TimeSpan TimeIncrease { get; }
    }
}