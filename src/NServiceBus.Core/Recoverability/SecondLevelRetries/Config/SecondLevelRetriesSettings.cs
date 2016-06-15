namespace NServiceBus.SecondLevelRetries.Config
{
    using System;

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
            ReplacementTypeOrMember = "CustomRetryPolicy(Func<SecondLevelRetryContext, TimeSpan> customPolicy)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register a custom retry policy. The callback receives the failed message, the exception and the current second level retry attempt.
        /// </summary>
        /// <param name="customPolicy">the function which is invoked on a failed message to determine the delay until the message is retried.</param>
        public void CustomRetryPolicy(Func<SecondLevelRetryContext, TimeSpan> customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            config.Settings.Set("SecondLevelRetries.RetryPolicy", customPolicy);
        }

        EndpointConfiguration config;
    }
}