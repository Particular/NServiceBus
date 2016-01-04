namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using System.Threading.Tasks;
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
        public void SetRetryNotification(Func<FirstLevelRetry, Task> action)
        {
            Guard.AgainstNull(nameof(action), action);
            var settings = busConfiguration.Settings;
            settings.SetFirstLevelRetryNotification(action);
        }
    }
}