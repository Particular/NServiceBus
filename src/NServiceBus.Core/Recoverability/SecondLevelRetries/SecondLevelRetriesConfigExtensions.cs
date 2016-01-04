namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Settings;
    using SecondLevelRetries.Config;

    /// <summary>
    /// Provides config options for the SLR feature.
    /// </summary>
    public static class SecondLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the second level retries.
        /// </summary>
        /// <param name="busConfiguration">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static SecondLevelRetriesSettings SecondLevelRetries(this BusConfiguration busConfiguration)
        {
            Guard.AgainstNull(nameof(busConfiguration), busConfiguration);
            return new SecondLevelRetriesSettings(busConfiguration);
        }

        internal static void SetSecondLevelRetryNotification(this SettingsHolder settings, Func<SecondLevelRetry, Task> action)
        {
            settings.Set<Func<SecondLevelRetry, Task>>(action);
        }

        internal static Func<SecondLevelRetry, Task> GetSecondLevelRetryNotification(this ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<SecondLevelRetry, Task>>();
        }
    }
}