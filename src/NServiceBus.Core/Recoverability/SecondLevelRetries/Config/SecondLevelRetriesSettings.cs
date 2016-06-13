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
        /// Register a custom retry policy. Overrides <see cref="NumberOfRetries"/> and <see cref="TimeIncrease"/> configuration.
        /// </summary>
        public SecondLevelRetriesSettings CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            config.Settings.Set(Recoverability.SlrCustomPolicy, customPolicy);

            return this;
        }

        /// <summary>
        /// Configures the amount of times a message should be retried with a delay after failing all first level retries.
        /// </summary>
        /// <param name="numberOfRetries">The number of times to delay a failed a message.</param>
        public SecondLevelRetriesSettings NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);
            config.Settings.Set(Recoverability.SlrNumberOfRetries, numberOfRetries);

            return this;
        }

        /// <summary>
        /// Configures the delay after which a message should be retried again after failing all first level retries. The delay is multiplied by the number of the second level retry attempt.
        /// </summary>
        /// <param name="timeIncrease">The timespan to increase the delay for each second level retry attempt.</param>
        public SecondLevelRetriesSettings TimeIncrease(TimeSpan timeIncrease)
        {
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);
            config.Settings.Set(Recoverability.SlrTimeIncrease, timeIncrease);

            return this;
        }

        EndpointConfiguration config;
    }
}