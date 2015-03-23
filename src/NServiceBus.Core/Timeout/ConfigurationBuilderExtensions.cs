namespace NServiceBus
{
    using System;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class ConfigurationTimeoutExtensions
    {
        /// <summary>
        /// A critical error is raised when timeout retrieval fails.
        /// By default we wait for 2 seconds for the storage to come back.
        /// This method allows to change the default and extend the wait time.
        /// </summary>
        /// <param name="config">The instance of <see cref="BusConfiguration"/> to extend.</param>
        /// <param name="timeToWait">Time to wait before raising a critical error.</param>
        public static void TimeToWaitBeforeTriggeringCriticalErrorOnTimeoutOutages(this BusConfiguration config, TimeSpan timeToWait)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNegative(timeToWait, "timeToWait");
            config.Settings.Set("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", timeToWait);
        }
    }
}
