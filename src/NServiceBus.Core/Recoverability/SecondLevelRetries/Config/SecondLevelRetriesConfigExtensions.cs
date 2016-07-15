namespace NServiceBus
{
    /// <summary>
    /// Provides config options for the SLR feature.
    /// </summary>
    public static class SecondLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the second level retries.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "configuration.Recoverability().Delayed(delayed => )")]
        public static SecondLevelRetriesSettings SecondLevelRetries(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new SecondLevelRetriesSettings();
        }
    }
}