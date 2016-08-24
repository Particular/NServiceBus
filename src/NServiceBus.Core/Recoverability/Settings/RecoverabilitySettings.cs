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
        /// Disables legacy retries satellite (enabled by default). It prevents in-flight, retry messages from being left
        /// in .Retries queue when migrating from previous versions on NServiceBus. For further details can be found in V5 to V6 Upgrade Guide.
        /// </summary>
        [ObsoleteEx(
            Message = "Invocation of this method is no longer needed. Legacy retries satellites no longer exists starting from version 7.",
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0")]
        public RecoverabilitySettings DisableLegacyRetriesSatellite()
        {
            Settings.Set(Recoverability.DisableLegacyRetriesSatellite, true);

            return this;
        }
    }
}