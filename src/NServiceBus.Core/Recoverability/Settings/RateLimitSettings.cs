namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Configuration settings for rate limiting the endpoint when the system is experiencing multiple consecutive failures.
    /// </summary>
    public class RateLimitSettings : ExposeSettings
    {
        internal RateLimitSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of consecutive message failures that should occur before the endpoint starts rate limiting.
        /// </summary>
        public void ConsecutiveFailuresBeforeStartRateLimit(int consecutiveFailures)
        {
            Guard.AgainstNegative(nameof(consecutiveFailures), consecutiveFailures);

            var outageConfiguration = Settings.Get<RateLimitConfiguration>();
            outageConfiguration.NumberOfConsecutiveFailuresBeforeRateLimit = consecutiveFailures;
        }

        /// <summary>
        /// The amount time to wait between attempting to process another message when rate limiting.
        /// </summary>
        public void TimeToWaitBetweenRateLimitAttempts(TimeSpan waitPeriod)
        {
            Guard.AgainstNegative(nameof(waitPeriod), waitPeriod);

            var outageConfiguration = Settings.Get<RateLimitConfiguration>();
            outageConfiguration.WaitPeriodBetweenAttempts = waitPeriod;
        }

        /// <summary>
        /// Registers a callback which is invoked when the endpoint starts rate limiting.
        /// </summary>
        public RateLimitSettings RateLimitStarted(Func<Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var outageConfiguration = Settings.Get<RateLimitConfiguration>();
            outageConfiguration.RateLimitStartedNotification.Subscribe(enteredRateLimitMode =>
            {
                return notificationCallback();
            });

            return this;
        }

        /// <summary>
        /// Registers a callback which is invoked when the endpoint stops rate limiting.
        /// </summary>
        public RateLimitSettings RateLimitEnded(Func<Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var outageConfiguration = Settings.Get<RateLimitConfiguration>();
            outageConfiguration.RateLimitEndedNotification.Subscribe(endedRateLimitMode =>
            {
                return notificationCallback();
            });

            return this;
        }
    }
}