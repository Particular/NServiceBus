namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using Transports;

    /// <summary>
    /// Configuration settings for second level retries.
    /// </summary>
    public class SecondLevelRetriesSettings
    {
        internal SecondLevelRetriesSettings(EndpointConfiguration config)
        {
            this.config = config;
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
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            config.Settings.Set("SecondLevelRetries.RetryPolicy", customPolicy);
        }

        /// <summary>
        /// Disables second level retries.
        /// </summary>
        public void Disable()
        {
            config.Settings.Set(Recoverability.DelayedRetriesEnabled, false);
        }

        EndpointConfiguration config;
    }
}