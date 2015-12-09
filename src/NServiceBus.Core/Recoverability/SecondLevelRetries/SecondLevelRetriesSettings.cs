namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Transports;

    /// <summary>
    /// Configuration settings for second level retries.
    /// </summary>
    public class SecondLevelRetriesSettings
    {
        BusConfiguration busConfiguration;

        internal SecondLevelRetriesSettings(BusConfiguration busConfiguration)
        {
            this.busConfiguration = busConfiguration;
        }

        /// <summary>
        /// Register a custom retry policy.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7.0", 
            TreatAsErrorFromVersion = "6.0", 
            ReplacementTypeOrMember = "CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register a custom retry policy.
        /// </summary>
        public void CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)
        {
            Guard.AgainstNull("customPolicy", customPolicy);
            busConfiguration.Settings.Set("SecondLevelRetries.RetryPolicy", customPolicy);
        }

        /// <summary>
        /// Set a delegate that will be called when a message is sent to second level retires queue.
        /// </summary>
        public void AddRetryNotification(Action<SecondLevelRetry> action)
        {
            Guard.AgainstNull(nameof(action), action);
            var settings = busConfiguration.Settings;
            settings.AddNotifyOnSecondLevelRetry(action);
        }
    }
}