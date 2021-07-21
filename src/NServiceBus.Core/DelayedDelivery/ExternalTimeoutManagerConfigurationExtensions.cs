namespace NServiceBus.DelayedDelivery
{
    using Settings;

    /// <summary>
    /// Provides configuration extension to specify an external timeout manager address.
    /// </summary>
    public static class ExternalTimeoutManagerConfigurationExtensions
    {
        /// <summary>
        /// Configures this endpoint to use an external timeout manager to handle delayed messages.
        /// </summary>
        /// <param name="endpointConfiguration">The configuration to extend.</param>
        /// <param name="externalTimeoutManagerAddress">The address of the external timeout manager to use.</param>
        public static void UseExternalTimeoutManager(this EndpointConfiguration endpointConfiguration, string externalTimeoutManagerAddress)
        {
            Guard.AgainstNull(nameof(EndpointConfiguration), endpointConfiguration);
            Guard.AgainstNullAndEmpty(nameof(externalTimeoutManagerAddress), externalTimeoutManagerAddress);

            endpointConfiguration.Settings.Set(ExternalTimeoutManagerConfigurationKey, externalTimeoutManagerAddress);
        }

        internal static string GetExternalTimeoutManagerAddress(this SettingsHolder settings)
        {
            return settings.GetOrDefault<string>(ExternalTimeoutManagerConfigurationKey);
        }

        const string ExternalTimeoutManagerConfigurationKey = "NServiceBus.ExternalTimeoutManagerAddress";
    }
}