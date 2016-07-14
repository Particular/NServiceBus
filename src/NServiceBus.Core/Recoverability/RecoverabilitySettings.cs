namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using Settings;

    /// <summary>
    /// Configuration settings for recoverability
    /// </summary>
    public class RecoverabilitySettings : ExposeSettings
    {
        internal RecoverabilitySettings(SettingsHolder settings) : base(settings)
        {
            Immediate = new ImmediateRetriesSettings(settings);
            Delayed = new DelayedRetriesSettings(settings);
            Failed = new RetryFailedSettings(settings);
        }

        // TODO: Properties or methods?

        /// <summary>
        /// Exposes the immediate retries settings.
        /// </summary>
        public ImmediateRetriesSettings Immediate { get; private set; }

        /// <summary>
        /// Exposes the delayed retries settings.
        /// </summary>
        public DelayedRetriesSettings Delayed { get; private set; }

        /// <summary>
        /// Exposes the retry failed settings.
        /// </summary>
        public RetryFailedSettings Failed { get; private set; }
    }

    /// <summary>
    /// Extension methods for recoverability which extend <see cref="EndpointConfiguration"/>
    /// </summary>
    public static class RecoverabilityEndpointConfigurationExtensions
    {
        /// <summary>
        /// Configuration settings for recoverability
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        public static RecoverabilitySettings Recoverability(this EndpointConfiguration configuration)
        {
            return new RecoverabilitySettings(configuration.GetSettings());
        }
    }

    /// <summary>
    /// Configuration settings for immediate retries
    /// </summary>
    public class ImmediateRetriesSettings : ExposeSettings
    {
        internal ImmediateRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of times a message should be immediately retried after failing before escalating to second level retries.
        /// </summary>
        /// <param name="numberOfRetries">The number of times to immediately retry a failed message.</param>
        public ImmediateRetriesSettings NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(Recoverability.FlrNumberOfRetries, numberOfRetries);

            return this;
        }

        /// <summary>
        /// Configures NServiceBus to not retry failed messages using the first level retry mechanism.
        /// </summary>
        public ImmediateRetriesSettings Disable()
        {
            Settings.Set(Recoverability.FlrNumberOfRetries, 0);

            return this;
        }
    }

    /// <summary>
    /// Configuration settings for delayed retries.
    /// </summary>
    public class DelayedRetriesSettings : ExposeSettings
    {
        internal DelayedRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the number of times a message should be retried with a delay after failing first level retries.
        /// </summary>
        public DelayedRetriesSettings NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(Recoverability.SlrNumberOfRetries, numberOfRetries);

            return this;
        }

        /// <summary>
        /// Configures the delay interval increase for each failed second level retry attempt.
        /// </summary>
        public DelayedRetriesSettings TimeIncrease(TimeSpan timeIncrease)
        {
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);

            Settings.Set(Recoverability.SlrTimeIncrease, timeIncrease);

            return this;
        }

        /// <summary>
        /// Configures NServiceBus to not retry failed messages using the second level retry mechanism.
        /// </summary>
        public DelayedRetriesSettings Disable()
        {
            Settings.Set(Recoverability.SlrNumberOfRetries, 0);

            return this;
        }
    }

    /// <summary>
    /// Configuration settings for retry faults
    /// </summary>
    public class RetryFailedSettings : ExposeSettings
    {
        internal RetryFailedSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures a header customization action which gets called after all fault headers have been applied.
        /// </summary>
        /// <param name="customization">The customization action.</param>
        public RetryFailedSettings HeaderCustomization(Action<Dictionary<string, string>> customization)
        {
            Guard.AgainstNull(nameof(customization), customization);

            Settings.Set(Recoverability.FaultHeaderCustomization, customization);

            return this;
        }
    }
}