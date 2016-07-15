namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
    using Settings;
    using Transport;

    /// <summary>
    /// Configuration settings for recoverability.
    /// </summary>
    public class RecoverabilitySettings : ExposeSettings
    {
        internal RecoverabilitySettings(SettingsHolder settings) : base(settings)
        {
            Failed = new RetryFailedSettings(settings);
        }

        /// <summary>
        /// Exposes the retry failed settings.
        /// </summary>
        public RetryFailedSettings Failed { get; private set; }

        /// <summary>
        /// Exposes the immediate retries settings.
        /// </summary>
        public RecoverabilitySettings Immediate(Action<ImmediateRetriesSettings> customizations)
        {
            customizations(new ImmediateRetriesSettings(Settings));
            return this;
        }

        /// <summary>
        /// Exposes the delayed retries settings.
        /// </summary>
        public RecoverabilitySettings Delayed(Action<DelayedRetriesSettings> customizations)
        {
            customizations(new DelayedRetriesSettings(Settings));
            return this;
        }

        /// <summary>
        /// Configures a custom recoverability policy. It allows to take full control over the recoverability decision process.
        /// </summary>
        /// <param name="custom">The custom recoverability.</param>
        public RecoverabilitySettings CustomPolicy(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> custom)
        {
            Guard.AgainstNull(nameof(custom), custom);

            Settings.Set(Recoverability.PolicyOverride, custom);

            return this;
        }
    }

    /// <summary>
    /// Provides information about the recoverability configuration.
    /// </summary>
    public struct RecoverabilityConfig
    {
        internal RecoverabilityConfig(ImmediateConfig immediateConfig, DelayedConfig delayedConfig)
        {
            Immediate = immediateConfig;
            Delayed = delayedConfig;
        }

        /// <summary>
        /// Exposes the immediate retries configuration.
        /// </summary>
        public ImmediateConfig Immediate { get; }

        /// <summary>
        /// Exposes the delayed retries configuration.
        /// </summary>
        public DelayedConfig Delayed { get; }
    }

    /// <summary>
    /// Provides information about the immediate retries configuration.
    /// </summary>
    public struct ImmediateConfig
    {
        internal ImmediateConfig(int maxNumberOfRetries)
        {
            MaxNumberOfRetries = maxNumberOfRetries;
        }

        /// <summary>
        /// Gets the configured maximum number of immediate retries.
        /// </summary>
        /// <remarks>Zero means no retries possible.</remarks>
        public int MaxNumberOfRetries { get; }
    }

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

    /// <summary>
    /// Extension methods for recoverability which extend <see cref="EndpointConfiguration" />.
    /// </summary>
    public static class RecoverabilityEndpointConfigurationExtensions
    {
        /// <summary>
        /// Configuration settings for recoverability.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        public static RecoverabilitySettings Recoverability(this EndpointConfiguration configuration)
        {
            return new RecoverabilitySettings(configuration.GetSettings());
        }
    }

    /// <summary>
    /// Configuration settings for immediate retries.
    /// </summary>
    public class ImmediateRetriesSettings : ExposeSettings
    {
        internal ImmediateRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of times a message should be immediately retried after failing before escalating to second level
        /// retries.
        /// </summary>
        /// <param name="numberOfRetries">The number of times to immediately retry a failed message.</param>
        public void NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(Recoverability.FlrNumberOfRetries, numberOfRetries);
        }

        /// <summary>
        /// Configures NServiceBus to not retry failed messages using the first level retry mechanism.
        /// </summary>
        public void Disable()
        {
            Settings.Set(Recoverability.FlrNumberOfRetries, 0);
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
        public void Disable()
        {
            Settings.Set(Recoverability.SlrNumberOfRetries, 0);
        }
    }

    /// <summary>
    /// Configuration settings for retry faults.
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