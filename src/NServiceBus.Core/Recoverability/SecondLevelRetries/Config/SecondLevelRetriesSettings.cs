namespace NServiceBus.SecondLevelRetries.Config
{
    using System;
    using Transports;

    /// <summary>
    ///     Configuration settings for second level retries.
    /// </summary>
    public class SecondLevelRetriesSettings
    {
        internal SecondLevelRetriesSettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        ///     Register a custom retry policy.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Register a custom retry policy.
        /// </summary>
        public void CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)
        {
            Guard.AgainstNull("customPolicy", customPolicy);
            config.Settings.Set("SecondLevelRetries.RetryPolicy", customPolicy);
        }

        BusConfiguration config;
    }
}