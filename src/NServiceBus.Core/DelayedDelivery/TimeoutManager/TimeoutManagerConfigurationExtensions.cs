namespace NServiceBus
{
    using Settings;
    using Transport;

    /// <summary>
    /// Extensions to configure the timeout manager via <see cref="TimeoutManagerConfiguration"/>.
    /// </summary>
    public static class TimeoutManagerConfigurationExtensions
    {
        const string TimeoutManagerMaxConcurrencySettingsKey = "NServiceBus.TimeoutManager.MaxConcurrency";

        /// <summary>
        /// Configures the timeout manager.
        /// </summary>
        public static TimeoutManagerConfiguration TimeoutManager(this EndpointConfiguration endpointConfiguration)
        {
            return new TimeoutManagerConfiguration(endpointConfiguration.Settings);
        }

        /// <summary>
        /// Configures the allowed number of concurrent messages for the timeout manager's satellite queues. The default value is specified in <see cref="PushRuntimeSettings.Default"/>.
        /// </summary>
        /// <param name="timeoutManagerConfiguration">The settings to extend.</param>
        /// <param name="maxConcurrency">The maximum number of processed messages per satellite queue.</param>
        public static void LimitMessageProcessingConcurrencyTo(this TimeoutManagerConfiguration timeoutManagerConfiguration, int maxConcurrency)
        {
            timeoutManagerConfiguration.settings.Set(TimeoutManagerMaxConcurrencySettingsKey, new PushRuntimeSettings(maxConcurrency));
        }

        internal static PushRuntimeSettings GetTimeoutManagerMaxConcurrency(this ReadOnlySettings settings)
        {
            return settings.GetOrDefault<PushRuntimeSettings>(TimeoutManagerMaxConcurrencySettingsKey) ?? PushRuntimeSettings.Default;
        }
    }
}