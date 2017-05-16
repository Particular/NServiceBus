namespace NServiceBus
{
    using System;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class ConfigurationTimeoutExtensions
    {
        /// <summary>
        /// Configures the amount of time to wait after a timeout retrieval fails before triggering a critical error.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="timeToWait">The amount of time to wait before triggering a critical error.</param>
        public static void TimeToWaitBeforeTriggeringCriticalErrorOnTimeoutOutages(this EndpointConfiguration config, TimeSpan timeToWait)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNegative(nameof(timeToWait), timeToWait);
            config.Settings.Set("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", timeToWait);
        }
    }
}
