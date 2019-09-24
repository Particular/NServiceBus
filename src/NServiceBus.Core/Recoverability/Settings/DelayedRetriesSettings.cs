namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Configuration settings for Delayed Retries.
    /// </summary>
    public class DelayedRetriesSettings : ExposeSettings
    {
        internal DelayedRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the number of times a message should be retried with a delay after failing Immediate Retries.
        /// </summary>
        public DelayedRetriesSettings NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(RecoverabilityComponent.NumberOfDelayedRetries, numberOfRetries);

            return this;
        }

        /// <summary>
        /// Configures the delay interval increase for each failed Delayed Retries attempt.
        /// </summary>
        public DelayedRetriesSettings TimeIncrease(TimeSpan timeIncrease)
        {
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);

            Settings.Set(RecoverabilityComponent.DelayedRetriesTimeIncrease, timeIncrease);

            return this;
        }
    }
}