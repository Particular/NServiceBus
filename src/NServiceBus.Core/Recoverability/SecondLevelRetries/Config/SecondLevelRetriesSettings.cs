namespace NServiceBus
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
        /// Registers a custom retry policy.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "configuration.Recoverability().PolicyOverride(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> @override)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Registers a custom retry policy. The callback receives the failed message, the exception, and the current second level retry attempt.
        /// </summary>
        /// <param name="customPolicy">The function that is invoked on a failed message to determine the delay until the message is retried.</param>
        public SecondLevelRetriesSettings CustomRetryPolicy(Func<SecondLevelRetryContext, TimeSpan> customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            config.Settings.Set(Recoverability.SlrCustomPolicy, customPolicy);

            return this;
        }

        EndpointConfiguration config;
    }
}