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
            ReplacementTypeOrMember = "CustomRetryPolicy(Func<SecondLevelRetryContext, TimeSpan> customPolicy)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Registers a custom retry policy. The callback receives the failed message, the exception, and the current second level retry attempt. Overrides <see cref="NumberOfRetries"/> and <see cref="TimeIncrease"/> configuration.
        /// </summary>
        /// <param name="customPolicy">The function that is invoked on a failed message to determine the delay until the message is retried.</param>
        public SecondLevelRetriesSettings CustomRetryPolicy(Func<SecondLevelRetryContext, TimeSpan> customPolicy)
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