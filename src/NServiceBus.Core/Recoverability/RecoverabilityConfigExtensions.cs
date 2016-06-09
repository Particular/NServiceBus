namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Provides config options for the Recoverability feature.
    /// </summary>
    public static class RecoverabilityConfigExtensions
    {
        /// <summary>
        /// Disables the first level retries.
        /// </summary>
        public static void DisableFirstLevelRetries(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);

            config.GetSettings().Set(Recoverability.ImmedidateRetriesEnabled, false);
        }
    }
}