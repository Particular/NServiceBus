namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Configuration settings for Immediate Retries.
    /// </summary>
    public class ImmediateRetriesSettings : ExposeSettings
    {
        internal ImmediateRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of times a message should be immediately retried after failing
        /// before escalating to Delayed Retries.
        /// </summary>
        /// <param name="numberOfRetries">The number of times to immediately retry a failed message.</param>
        public void NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(RecoverabilityComponent.NumberOfImmediateRetries, numberOfRetries);
        }
    }
}