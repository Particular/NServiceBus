namespace NServiceBus
{
    using System;
    using Recoverability.Settings;

    /// <summary>
    /// Provides information about the delayed retries configuration.
    /// </summary>
    public class DelayedConfig
    {
        /// <summary>
        /// Creates a new delayed retries configuration.
        /// </summary>
        /// <param name="maxNumberOfRetries">The maximum number of delayed retries.</param>
        /// <param name="timeIncrease">The time of increase for individual delayed retries.</param>
        public DelayedConfig(int maxNumberOfRetries, TimeSpan timeIncrease) : this(maxNumberOfRetries, timeIncrease, null, null)
        {
            Guard.AgainstNegative(nameof(maxNumberOfRetries), maxNumberOfRetries);
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);

            MaxNumberOfRetries = maxNumberOfRetries;
            TimeIncrease = timeIncrease;
        }

        /// <summary>
        /// Creates a new delayed retries configuration.
        /// </summary>
        /// <param name="maxNumberOfRetries">The maximum number of delayed retries.</param>
        /// <param name="timeIncrease">The time of increase for individual delayed retries.</param>
        /// <param name="maxAttemptsOnHttpRateLimitExceptions">Specifies the max amount of retry attempts when running into HTTP rate limiting exceptions.</param>
        /// <param name="rateLimitStrategies">The rate limit strategies to apply.</param>
        public DelayedConfig(int maxNumberOfRetries, TimeSpan timeIncrease, int? maxAttemptsOnHttpRateLimitExceptions, IHttpRateLimitStrategy[] rateLimitStrategies = null)
        {
            Guard.AgainstNegative(nameof(maxNumberOfRetries), maxNumberOfRetries);
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);

            MaxNumberOfRetries = maxNumberOfRetries;
            TimeIncrease = timeIncrease;
            HttpRateLimitingEnabled = maxAttemptsOnHttpRateLimitExceptions.HasValue;
            MaxAttemptsOnHttpRateLimitExceptions = maxAttemptsOnHttpRateLimitExceptions;
            RateLimitStrategies = rateLimitStrategies ?? Array.Empty<IHttpRateLimitStrategy>();
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

        /// <summary>
        /// Specifies whether HTTP Rate limiting has been enabled.
        /// </summary>
        public bool HttpRateLimitingEnabled { get; }

        /// <summary>
        /// Specifies the max amount of retry attempts when running into HTTP rate limiting exceptions.
        /// </summary>
        public int? MaxAttemptsOnHttpRateLimitExceptions { get; }

        /// <summary>
        /// The rate-limiting strategies to apply.
        /// </summary>
        public IHttpRateLimitStrategy[] RateLimitStrategies { get; }
    }
}