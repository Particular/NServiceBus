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
        /// Configures the amount of consecutive message failures that should occur before the endpoint switches to system outage mode.
        /// </summary>
        /// <param name="consecutiveFailuresBeforeSystemOutage">The number of consecutive failures that must occur before triggering system outage mode.</param>
        public void NumberOfConsecutiveFailuresBeforeSystemOutage(int consecutiveFailuresBeforeSystemOutage)
        {
            Guard.AgainstNegative(nameof(consecutiveFailuresBeforeSystemOutage), consecutiveFailuresBeforeSystemOutage);

            var outageConfiguration = Settings.Get<SystemOutageConfiguration>();
            outageConfiguration.NumberOfConsecutiveFailuresBeforeThrottling = consecutiveFailuresBeforeSystemOutage;
        }

        /// <summary>
        /// The amount time to wait between attempting to process another message in system outage mode.
        /// </summary>
        /// <param name="waitPeriodBetweenAttempts">The time to wait before attempting to process another message in system outage mode.</param>
        public void TimeToWaitBetweenSystemOutageProcessingAttempts(TimeSpan waitPeriodBetweenAttempts)
        {
            Guard.AgainstNegative(nameof(waitPeriodBetweenAttempts), waitPeriodBetweenAttempts);

            var outageConfiguration = Settings.Get<SystemOutageConfiguration>();
            outageConfiguration.WaitPeriodBetweenAttempts = waitPeriodBetweenAttempts;
        }

        /// <summary>
        /// Registers a callback which is invoked when the endpoint detects system outage mode.
        /// </summary>
        public SystemOutageSettings OnSystemOutageStarted(Func<Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var outageConfiguration = Settings.Get<SystemOutageConfiguration>();
            outageConfiguration.ThrottledModeStartedNotification.Subscribe(enteredThrottledMode =>
            {
                return notificationCallback();
            });

            return this;
        }

        /// <summary>
        /// Registers a callback which is invoked when the endpoint stops system outage mode.
        /// </summary>
        public SystemOutageSettings OnSystemOutageEnded(Func<Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var outageConfiguration = Settings.Get<SystemOutageConfiguration>();
            outageConfiguration.ThrottledModeEndedNotification.Subscribe(endedThrottledMode =>
            {
                return notificationCallback();
            });

            return this;
        }
    }
}