namespace NServiceBus
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
        /// Register a custom retry policy. Overrides <see cref="NumberOfRetries"/> and <see cref="TimeIncrease"/> configuration.
        /// </summary>
        public SecondLevelRetriesSettings CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            config.Settings.Set(Recoverability.SlrCustomPolicy, customPolicy);

            return this;
        }

        /// <summary>
        /// Configures the number of times a message should be retried with a delay after failing first level retries.
        /// </summary>
        public SecondLevelRetriesSettings NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);
            config.Settings.Set(Recoverability.SlrNumberOfRetries, numberOfRetries);

            return this;
        }

        /// <summary>
        /// Configures the delay interval increase for each failed second level retry attempt.
        /// </summary>
        public SecondLevelRetriesSettings TimeIncrease(TimeSpan timeIncrease)
        {
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);
            config.Settings.Set(Recoverability.SlrTimeIncrease, timeIncrease);

            return this;
        }

        /// <summary>
        /// Configures NServiceBus to not retry failed messages using the second level retry mechanism.
        /// </summary>
        public SecondLevelRetriesSettings Disable()
        {
            config.Settings.Set(Recoverability.SlrNumberOfRetries, 0);

            return this;
        }

        EndpointConfiguration config;
    }
}