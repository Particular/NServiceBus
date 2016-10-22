namespace NServiceBus
{
    using System;
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
        }

        /// <summary>
        /// Exposes the retry failed settings.
        /// </summary>
        public RecoverabilitySettings Failed(Action<RetryFailedSettings> customizations)
        {
            customizations(new RetryFailedSettings(Settings));
            return this;
        }

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

        /// <summary>
        /// Disables the legacy retries satellite. The retries satellite is enabled by default to prevent in-flight retry messages from being left
        /// in the .Retries queue when migrating from previous versions of NServiceBus. Further details can be found in the V5 to V6 Upgrade Guide.
        /// </summary>
        public RecoverabilitySettings DisableLegacyRetriesSatellite()
        {
            Settings.Set(Recoverability.DisableLegacyRetriesSatellite, true);

            return this;
        }
    }
}