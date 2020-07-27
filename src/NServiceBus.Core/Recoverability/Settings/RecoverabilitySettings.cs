namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Settings;
    using Transport;

    /// <summary>
    /// Configuration settings for recoverability.
    /// </summary>
    public class RecoverabilitySettings : ExposeSettings
    {
        internal RecoverabilitySettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Exposes the retry failed settings.
        /// </summary>
        public RecoverabilitySettings Failed(Action<RetryFailedSettings> customizations)
        {
            Guard.AgainstNull(nameof(customizations), customizations);
            customizations(new RetryFailedSettings(Settings));
            return this;
        }

        /// <summary>
        /// Exposes the immediate retries settings.
        /// </summary>
        public RecoverabilitySettings Immediate(Action<ImmediateRetriesSettings> customizations)
        {
            Guard.AgainstNull(nameof(customizations), customizations);
            customizations(new ImmediateRetriesSettings(Settings));
            return this;
        }

        /// <summary>
        /// Exposes the delayed retries settings.
        /// </summary>
        public RecoverabilitySettings Delayed(Action<DelayedRetriesSettings> customizations)
        {
            Guard.AgainstNull(nameof(customizations), customizations);
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

            Settings.Set(RecoverabilityComponent.PolicyOverride, custom);

            return this;
        }

        /// <summary>
        /// Adds the specified exception type to be treated as an unrecoverable exception.
        /// </summary>
        /// <typeparam name="T">The exception type.</typeparam>
        public RecoverabilitySettings AddUnrecoverableException<T>() where T: Exception
        {
            Settings.AddUnrecoverableException(typeof(T));
            return this;
        }

        /// <summary>
        /// Adds the specified exception type to be treated as an unrecoverable exception.
        /// </summary>
        /// <param name="exceptionType">The exception type.</param>
        public RecoverabilitySettings AddUnrecoverableException(Type exceptionType)
        {
            Guard.AgainstNull(nameof(exceptionType), exceptionType);
            Settings.AddUnrecoverableException(exceptionType);
            return this;
        }
    }
}