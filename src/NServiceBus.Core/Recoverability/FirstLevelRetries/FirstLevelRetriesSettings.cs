namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using NServiceBus.Faults;

    /// <summary>
    /// Configuration settings for first level retries.
    /// </summary>
    public class FirstLevelRetriesSettings
    {
        BusConfiguration busConfiguration;

        internal FirstLevelRetriesSettings(BusConfiguration busConfiguration)
        {
            this.busConfiguration = busConfiguration;
        }


        /// <summary>
        /// Set a delegate that will be called when a first level retry occurs.
        /// </summary>
        public void AddRetryNotification(Action<FirstLevelRetry> action)
        {
            Guard.AgainstNull(nameof(action), action);
            var settings = busConfiguration.Settings;
            settings.AddNotifyOnFirstLevelRetry(action);
        }
    }
}