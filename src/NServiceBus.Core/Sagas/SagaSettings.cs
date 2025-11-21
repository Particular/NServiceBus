namespace NServiceBus.Sagas
{
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Custom settings related to the outbox feature.
    /// </summary>
    public class SagaSettings : ExposeSettings
    {
        internal const string DisableVerifyingIfEntitiesAreShared = "Sagas.DisableVerifyingIfEntitiesAreShared";
        internal const string DisableSagaScanningKey = "Sagas.DisableSagaScanning";

        internal SagaSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Disabled the best practice validator to guard against sharing saga data entities between different saga types.
        /// </summary>
        public void DisableBestPracticeValidation()
        {
            Settings.Set(DisableVerifyingIfEntitiesAreShared, true);
        }

        /// <summary>
        /// Disables assembly scanning for sagas. When disabled, sagas must be manually registered using <see cref="SagaRegistrationExtensions.AddSaga{TSaga}" />.
        /// </summary>
        public void DisableSagaScanning()
        {
            Settings.Set(DisableSagaScanningKey, true);
        }
    }
}

namespace NServiceBus
{
    using System;
    using Sagas;

    /// <summary>
    /// Configuration methods for Sagas.
    /// </summary>
    public static class SagasConfigExtensions
    {
        /// <summary>
        /// Configures the saga feature.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static SagaSettings Sagas(this EndpointConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return new SagaSettings(config.Settings);
        }
    }
}