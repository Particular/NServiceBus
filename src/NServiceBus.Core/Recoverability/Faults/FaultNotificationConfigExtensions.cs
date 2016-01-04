namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides config options for the faults feature.
    /// </summary>
    public static class FaultNotificationConfigExtensions
    {

        /// <summary>
        /// Allows for customization of the faults.
        /// </summary>
        /// <param name="busConfiguration">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static FaultsSettings Faults(this BusConfiguration busConfiguration)
        {
            Guard.AgainstNull("busConfiguration", busConfiguration);
            return new FaultsSettings(busConfiguration);
        }

        internal static void SetFailedMessageNotification(this SettingsHolder settings, Func<FailedMessage, Task> action)
        {
            settings.Set<Func<FailedMessage, Task>>(action);
        }

        internal static Func<FailedMessage, Task> GetFailedMessageNotification(this ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<FailedMessage, Task>>();
        }

    }
}