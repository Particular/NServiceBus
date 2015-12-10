namespace NServiceBus.Faults
{
    using System;

    /// <summary>
    /// Configuration settings for faults.
    /// </summary>
    public class FaultsSettings
    {
        BusConfiguration busConfiguration;

        internal FaultsSettings(BusConfiguration busConfiguration)
        {
            this.busConfiguration = busConfiguration;
        }

        /// <summary>
        /// Set a delegate that will be called when a first level retry occurs.
        /// </summary>
        public void AddFaultNotification(Action<FailedMessage> action)
        {
            Guard.AgainstNull(nameof(action), action);
            var settings = busConfiguration.Settings;
            settings.AddNotifyOnFailedMessage(action);
        }
    }
}