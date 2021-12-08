namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration settings for rate limiting the endpoint when the system is experiencing multiple consecutive failures.
    /// </summary>
    public class RateLimitSettings
    {
        /// <summary>
        /// Configuration settings for rate limiting the endpoint when the system is experiencing multiple consecutive failures.
        /// </summary>
        public RateLimitSettings(TimeSpan? timeToWaitBetweenThrottledAttempts = null, Func<CancellationToken, Task> onRateLimitStarted = null, Func<CancellationToken, Task> onRateLimitEnded = null)
        {
            TimeToWaitBetweenThrottledAttempts = timeToWaitBetweenThrottledAttempts ?? TimeSpan.FromSeconds(1);
            OnRateLimitStarted = onRateLimitStarted;
            OnRateLimitEnded = onRateLimitEnded;
        }

        /// <summary>
        /// The amount of time to wait between message processing attempts when in rate limited mode.
        /// </summary>
        public TimeSpan TimeToWaitBetweenThrottledAttempts { get; }

        /// <summary>
        /// A callback for when rate limiting is started.
        /// </summary>
        public Func<CancellationToken, Task> OnRateLimitStarted { get; }

        /// <summary>
        /// A callback for then rate limiting is ended.
        /// </summary>
        public Func<CancellationToken, Task> OnRateLimitEnded { get; }
    }
}