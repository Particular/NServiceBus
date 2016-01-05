namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.SecondLevelRetries.Config;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides config options for the FLR feature.
    /// </summary>
    public static class FirstLevelRetryConfigExtensions
    {

        /// <summary>
        /// Allows for customization of the first level retries.
        /// </summary>
        /// <param name="busConfiguration">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static FirstLevelRetriesSettings FirstLevelRetries(this BusConfiguration busConfiguration)
        {
            Guard.AgainstNull("busConfiguration", busConfiguration);
            return new FirstLevelRetriesSettings(busConfiguration);
        }

        internal static void SetFirstLevelRetryNotification(this SettingsHolder settings, Func<FirstLevelRetry, Task> action)
        {
            settings.Set<Func<FirstLevelRetry, Task>>(action);
        }

        internal static Func<FirstLevelRetry, Task> GetFirstLevelRetryNotification(this ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<FirstLevelRetry, Task>>();
        }

    }
}