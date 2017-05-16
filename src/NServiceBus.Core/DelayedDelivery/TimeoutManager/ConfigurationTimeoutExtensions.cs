namespace NServiceBus
{
    using System;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class ConfigurationTimeoutExtensions
    {
        /// <summary>
        /// A critical error is raised when timeout retrieval fails over a certain period of time.
        /// This method allows to change the default and extend the wait time before raising a critical error.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="timeToWait">Time to wait before raising a critical error.</param>
        public static void TimeToWaitBeforeTriggeringCriticalErrorOnTimeoutOutages(this EndpointConfiguration config, TimeSpan timeToWait)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNegative(nameof(timeToWait), timeToWait);
            config.Settings.Set("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", timeToWait);
        }
    }
}