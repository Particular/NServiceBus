namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Configuration settings for when a system outage is detected.
    /// </summary>
    public class SystemOutageSettings : ExposeSettings
    {
        internal SystemOutageSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of consecutive message failures that should occur before throttling of the endpoint begins.
        /// </summary>
        /// <param name="consecutiveFailuresBeforeThrottling">The number of times to immediately retry a failed message.</param>
        public void NumberOfConsecutiveFailuresBeforeThrottling(int consecutiveFailuresBeforeThrottling)
        {
            Guard.AgainstNegative(nameof(consecutiveFailuresBeforeThrottling), consecutiveFailuresBeforeThrottling);

            // Settings.Set(RecoverabilityComponent.NumberOfImmediateRetries, numberOfRetries);
        }

        /// <summary>
        /// The amount time to wait between attempting to process another message in throttled mode.
        /// </summary>
        /// <param name="waitPeriodBetweenAttempts">The time to wait before attempting to process another message in throttled mode.</param>
        public void TimeToWaitBeforeThrottledProcessingAttempts(TimeSpan waitPeriodBetweenAttempts)
        {
            Guard.AgainstNegative(nameof(waitPeriodBetweenAttempts), waitPeriodBetweenAttempts);

            // Settings.Set(RecoverabilityComponent.NumberOfImmediateRetries, numberOfRetries);
        }

        /// <summary>
        /// The number of messages to process concurrently when in throttled mode.
        /// </summary>
        /// <param name="throttledConcurrency">The number of messages to process concurrently when in throttled mode.</param>
        public void ThrottledModeConcurrency(int throttledConcurrency)
        {
            Guard.AgainstNegative(nameof(throttledConcurrency), throttledConcurrency);

            // Settings.Set(RecoverabilityComponent.NumberOfImmediateRetries, numberOfRetries);
        }

        /// <summary>
        /// Registers a callback which is invoked when the endpoint enters throttled mode.
        /// </summary>
        public SystemOutageSettings OnThrottledModeStarted(Func<Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var outageConfiguration = Settings.Get<SystemOutageConfiguration>();
            outageConfiguration.ThrottledModeStartedNotification.Subscribe(enteredThrottledMode =>
            {
                return notificationCallback();
            });

            return this;
        }
    }
}