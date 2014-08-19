namespace NServiceBus.SecondLevelRetries.Config
{
    using System;

    /// <summary>
    /// Configuration settings for second level retries
    /// </summary>
    public class SecondLevelRetriesSettings
    {
        readonly ConfigurationBuilder config;

        internal SecondLevelRetriesSettings(ConfigurationBuilder config)
        {
            this.config = config;
        }

        /// <summary>
        /// Register a custom retry policy
        /// </summary>
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            config.Settings.Set("SecondLevelRetries.RetryPolicy", customPolicy);
        }
    }
}