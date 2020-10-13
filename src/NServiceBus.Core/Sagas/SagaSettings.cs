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
    }
}

namespace NServiceBus
{
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
            Guard.AgainstNull(nameof(config), config);
            return new SagaSettings(config.Settings);
        }
    }
}
